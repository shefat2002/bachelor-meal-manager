using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>P2-BE-1: POST/GET /api/meals — upsert semantics, RBAC (own meals only for members), filters/sort/pagination, tenant isolation.</summary>
public sealed class MealsFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static HttpRequestMessage TenantPost(string uri, object payload, Guid messId) => new(HttpMethod.Post, uri)
    {
        Content = JsonContent.Create(payload),
        Headers = { { "X-Mess-Id", messId.ToString() } },
    };

    [Fact]
    public async Task Submit_Upserts_And_Lists_With_Filters()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "01");
        var date = new DateOnly(2026, 11, 1);

        // Manager logs a full day for both members (guests folded into counts, 0.5 steps allowed)
        var submit = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new object[]
            {
                new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1.5m, dinner = 2m },
                new { memberId = mess.MemberId, breakfast = 0.5m, lunch = 1m, dinner = 1m },
            },
        }, mess.MessId));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await Body(submit);
        rows.EnumerateArray().Should().HaveCount(2);
        rows.EnumerateArray().Single(r => r.GetProperty("memberId").GetGuid() == mess.ManagerId)
            .GetProperty("totalUnits").GetDecimal().Should().Be(4.5m);

        // Member logs own meals for the next day → allowed
        var own = await mess.Member.SendAsync(TenantPost("/api/meals", new
        {
            date = date.AddDays(1),
            entries = new[] { new { memberId = mess.MemberId, breakfast = 0m, lunch = 1m, dinner = 0.5m } },
        }, mess.MessId));
        own.StatusCode.Should().Be(HttpStatusCode.OK);

        // Re-submitting the same (member, date) overwrites instead of duplicating
        var rowId = rows.EnumerateArray().Single(r => r.GetProperty("memberId").GetGuid() == mess.MemberId)
            .GetProperty("id").GetGuid();
        var resubmit = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new[] { new { memberId = mess.MemberId, breakfast = 0m, lunch = 0m, dinner = 3m } },
        }, mess.MessId));
        resubmit.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(resubmit)).EnumerateArray().Single().GetProperty("id").GetGuid().Should().Be(rowId);

        // List: date range covers all three rows; memberId filter narrows to one
        var list = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/meals?from=2026-11-01&to=2026-11-02&sortBy=date&sortDir=asc")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var page = await Body(list);
        page.GetProperty("totalCount").GetInt32().Should().Be(3);

        var mine = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/meals?memberId={mess.MemberId}")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var memberRows = await Body(mine);
        memberRows.GetProperty("totalCount").GetInt32().Should().Be(2);

        // Daily-sheet style exact-date query
        var day = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/meals?from=2026-11-01&to=2026-11-01&pageSize=100")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        (await Body(day)).GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Member_Cannot_Log_Other_Members_Meals()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "02");

        var response = await mess.Member.SendAsync(TenantPost("/api/meals", new
        {
            date = new DateOnly(2026, 11, 1),
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1m, dinner = 1m } },
        }, mess.MessId));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Body(response)).GetProperty("type").GetString().Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task Submit_Validates_Counts_And_Entries()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "03");
        var date = new DateOnly(2026, 11, 1);

        var badStep = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 0.3m, lunch = 1m, dinner = 1m } },
        }, mess.MessId));
        badStep.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Body(badStep)).GetProperty("type").GetString().Should().Be("VALIDATION_ERROR");

        var tooMany = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 10.5m, lunch = 1m, dinner = 1m } },
        }, mess.MessId));
        tooMany.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var duplicate = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new object[]
            {
                new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1m, dinner = 1m },
                new { memberId = mess.ManagerId, breakfast = 0m, lunch = 0m, dinner = 0m },
            },
        }, mess.MessId));
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Target member must belong to the mess — cross-mess ids resolve to 404 (no leakage)
        var stranger = await mess.Manager.SendAsync(TenantPost("/api/meals", new
        {
            date,
            entries = new[] { new { memberId = Guid.NewGuid(), breakfast = 1m, lunch = 1m, dinner = 1m } },
        }, mess.MessId));
        stranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Validates_Query_Params()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "04");

        foreach (var query in new[] { "?from=2026-11-02&to=2026-11-01", "?sortBy=memberName", "?pageSize=101", "?sortDir=up" })
        {
            var response = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/meals{query}")
            {
                Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
            });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Body(response)).GetProperty("type").GetString().Should().Be("VALIDATION_ERROR");
        }
    }

    [Fact]
    public async Task Meals_Are_Tenant_Isolated()
    {
        var messA = await DomainTestSetup.CreateMessWithMemberAsync(factory, "05");
        var messB = await DomainTestSetup.CreateMessWithMemberAsync(factory, "06");
        var date = new DateOnly(2026, 11, 1);

        foreach (var (client, messId, memberId) in new[] { (messA.Manager, messA.MessId, messA.ManagerId), (messB.Manager, messB.MessId, messB.ManagerId) })
        {
            await client.SendAsync(TenantPost("/api/meals", new
            {
                date,
                entries = new[] { new { memberId, breakfast = 1m, lunch = 1m, dinner = 1m } },
            }, messId));
        }

        var listA = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/meals?pageSize=100")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        var pageA = await Body(listA);
        pageA.GetProperty("totalCount").GetInt32().Should().Be(1);
        pageA.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(r => r.GetProperty("memberId").GetGuid() == messA.ManagerId);

        // Mess A's member querying with Mess B's header → 403 (non-member), no rows leak
        var cross = await messA.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/meals")
        {
            Headers = { { "X-Mess-Id", messB.MessId.ToString() } },
        });
        cross.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
