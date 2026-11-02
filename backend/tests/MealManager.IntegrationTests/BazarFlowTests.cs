using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>P2-BE-2: POST/GET /api/expenses/bazar — manager-only writes, multi-item batches, filters/sort (date, line total), pagination, isolation.</summary>
public sealed class BazarFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Manager_Records_Bazar_Run_With_Multiple_Items()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "11");

        var create = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 1),
                items = new object[]
                {
                    new { name = "Rice", quantity = 10m, price = 65m },
                    new { name = "Eggs (dozen)", quantity = 2m, price = 140m },
                },
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location.Should().NotBeNull();
        var rows = await Body(create);
        rows.EnumerateArray().Should().HaveCount(2);
        rows.EnumerateArray().Single(r => r.GetProperty("itemName").GetString() == "Rice")
            .GetProperty("lineTotal").GetDecimal().Should().Be(650m);
        // Buyer defaults to the recording manager
        rows.EnumerateArray().Should().OnlyContain(r => r.GetProperty("buyerMemberId").GetGuid() == mess.ManagerId);

        // Buyer can be another active member
        var forMember = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 2),
                buyerMemberId = mess.MemberId,
                items = new[] { new { name = "Vegetables", quantity = 1m, price = 300m } },
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        forMember.StatusCode.Should().Be(HttpStatusCode.Created);

        // Unknown buyer → 404 masked
        var stranger = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 2),
                buyerMemberId = Guid.NewGuid(),
                items = new[] { new { name = "Fish", quantity = 1m, price = 500m } },
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        stranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Member_Cannot_Record_Bazar_Expenses()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "12");

        var response = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 1),
                items = new[] { new { name = "Rice", quantity = 1m, price = 65m } },
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Filters_Sorts_And_Paginates()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "13");

        for (var day = 1; day <= 3; day++)
        {
            await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
            {
                Content = JsonContent.Create(new
                {
                    date = new DateOnly(2026, 11, day),
                    items = new[] { new { name = $"Item {day}", quantity = day * 1m, price = 100m } },
                }),
                Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
            });
        }

        var list = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/expenses/bazar?from=2026-11-01&to=2026-11-03&sortBy=price&sortDir=desc")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var page = await Body(list);
        page.GetProperty("totalCount").GetInt32().Should().Be(3);
        // price sort = line total (qty × price): 300, 200, 100
        page.GetProperty("items").EnumerateArray()
            .Select(r => r.GetProperty("lineTotal").GetDecimal())
            .Should().ContainInOrder(300m, 200m, 100m);

        var buyerFilter = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/expenses/bazar?buyerMemberId={mess.MemberId}")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        (await Body(buyerFilter)).GetProperty("totalCount").GetInt32().Should().Be(0);

        var paged = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/expenses/bazar?sortBy=date&sortDir=asc&page=1&pageSize=2")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var firstPage = await Body(paged);
        firstPage.GetProperty("totalCount").GetInt32().Should().Be(3);
        firstPage.GetProperty("pageSize").GetInt32().Should().Be(2);
        firstPage.GetProperty("items").EnumerateArray().Should().HaveCount(2);

        var badSort = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/expenses/bazar?sortBy=buyer")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        badSort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Bazar_Expenses_Are_Tenant_Isolated()
    {
        var messA = await DomainTestSetup.CreateMessWithMemberAsync(factory, "14");
        var messB = await DomainTestSetup.CreateMessWithMemberAsync(factory, "15");

        foreach (var mess in new[] { messA, messB })
        {
            await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
            {
                Content = JsonContent.Create(new
                {
                    date = new DateOnly(2026, 11, 1),
                    items = new[] { new { name = "Rice", quantity = 1m, price = 65m } },
                }),
                Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
            });
        }

        var listA = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/expenses/bazar")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        var pageA = await Body(listA);
        pageA.GetProperty("totalCount").GetInt32().Should().Be(1);
        pageA.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(r => r.GetProperty("buyerMemberId").GetGuid() == messA.ManagerId);

        var cross = await messA.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/expenses/bazar")
        {
            Headers = { { "X-Mess-Id", messB.MessId.ToString() } },
        });
        cross.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
