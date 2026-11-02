using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>P2-BE-5: deposit ledger — member self-deposits (Pending) vs manager-created (auto-Approved), approve/reject, filters, isolation.</summary>
public sealed class DepositsFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Member_Deposit_Pending_Then_Manager_Approves()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "31");

        var create = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { amount = 2000m, date = new DateOnly(2026, 11, 3), note = "Cash" }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var deposit = await Body(create);
        deposit.GetProperty("status").GetString().Should().Be("Pending");
        deposit.GetProperty("memberName").GetString().Should().Be("Member Guy");
        var id = deposit.GetProperty("id").GetGuid();

        // Member cannot approve their own deposit
        var memberApprove = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{id}/approve")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        memberApprove.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Manager approves
        var approve = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{id}/approve")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Already approved → 409 on any further transition
        var reApprove = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{id}/approve")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        reApprove.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var reject = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{id}/reject")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        reject.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var list = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits?status=approved")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var page = await Body(list);
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
        page.GetProperty("items").EnumerateArray().Single().GetProperty("approvedBy").GetGuid()
            .Should().Be(mess.ManagerId);
    }

    [Fact]
    public async Task Manager_Created_Deposits_Are_AutoApproved()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "32");

        var create = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new
            {
                memberId = mess.MemberId,
                amount = 3500m,
                date = new DateOnly(2026, 11, 4),
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var deposit = await Body(create);
        deposit.GetProperty("status").GetString().Should().Be("Approved");
        deposit.GetProperty("approvedBy").GetGuid().Should().Be(mess.ManagerId);

        // Unknown target member → 404
        var stranger = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { memberId = Guid.NewGuid(), amount = 100m, date = new DateOnly(2026, 11, 4) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        stranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Member_Cannot_Deposit_For_Others_And_Manager_Can_Reject()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "33");

        var forOther = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { memberId = mess.ManagerId, amount = 100m, date = new DateOnly(2026, 11, 5) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        forOther.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var create = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { amount = 500m, date = new DateOnly(2026, 11, 5) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var id = (await Body(create)).GetProperty("id").GetGuid();

        var reject = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{id}/reject")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        reject.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits?status=rejected")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        (await Body(list)).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task List_Filters_Sorts_And_Paginates()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "34");

        await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { amount = 1000m, date = new DateOnly(2026, 11, 1) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { memberId = mess.MemberId, amount = 4000m, date = new DateOnly(2026, 11, 2) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });

        // sortBy=amount desc → manager's 4000 first; memberId filter narrows; status validation
        var byAmount = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/deposits?sortBy=amount&sortDir=desc")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var amounts = (await Body(byAmount)).GetProperty("items").EnumerateArray()
            .Select(d => d.GetProperty("amount").GetDecimal()).ToList();
        amounts.Should().ContainInOrder(4000m, 1000m);

        var mine = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/deposits?memberId={mess.MemberId}&status=approved")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        (await Body(mine)).GetProperty("totalCount").GetInt32().Should().Be(1);

        var badStatus = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits?status=zzz")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        badStatus.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposits_Are_Tenant_Isolated()
    {
        var messA = await DomainTestSetup.CreateMessWithMemberAsync(factory, "35");
        var messB = await DomainTestSetup.CreateMessWithMemberAsync(factory, "36");

        foreach (var mess in new[] { messA, messB })
        {
            await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
            {
                Content = JsonContent.Create(new { amount = 1234m, date = new DateOnly(2026, 11, 6) }),
                Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
            });
        }

        var listA = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        var pageA = await Body(listA);
        pageA.GetProperty("totalCount").GetInt32().Should().Be(1);
        pageA.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(d => d.GetProperty("memberId").GetGuid() == messA.MemberId);

        // Mess A's manager cannot approve Mess B's deposit (404 masks cross-tenant existence)
        var depositB = await messB.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits")
        {
            Headers = { { "X-Mess-Id", messB.MessId.ToString() } },
        });
        var idB = (await Body(depositB)).GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid();
        var cross = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{idB}/approve")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        cross.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
