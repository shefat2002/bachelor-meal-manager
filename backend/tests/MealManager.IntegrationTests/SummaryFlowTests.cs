using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>
/// P3-BE-3: GET /api/summary/messes/{messId} against a hand-computed dataset — the Phase 3 exit
/// criterion ("summary matches hand-computed values"). Also proves pending deposits are excluded
/// and that cache invalidation makes a post-write GET reflect new totals.
/// </summary>
public sealed class SummaryFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static HttpRequestMessage Tenant(HttpMethod method, string uri, object? payload, Guid messId)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Headers = { { "X-Mess-Id", messId.ToString() } },
        };
        if (payload is not null)
        {
            message.Content = JsonContent.Create(payload);
        }

        return message;
    }

    [Fact]
    public async Task Summary_Matches_Hand_Computed_Values()
    {
        // Seeded month (2026-11): manager M + members A, B.
        var (manager, memberA, memberB, mess, ids) = await SeedAsync(factory, "41");

        // Meals → units: A 5.5, B 6, M 2 (total 13.5)
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 11, 1),
            entries = new object[]
            {
                new { memberId = ids.ManagerId, breakfast = 1m, lunch = 1m, dinner = 0m },
                new { memberId = ids.MemberAId, breakfast = 1m, lunch = 1m, dinner = 1m },
                new { memberId = ids.MemberBId, breakfast = 1m, lunch = 0.5m, dinner = 0.5m },
            },
        }, mess));
        await memberA.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 11, 2),
            entries = new[] { new { memberId = ids.MemberAId, breakfast = 0.5m, lunch = 1m, dinner = 1m } },
        }, mess));
        await memberB.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 11, 3),
            entries = new[] { new { memberId = ids.MemberBId, breakfast = 2m, lunch = 1m, dinner = 1m } },
        }, mess));

        // Bazar → 930 total (650 on the 1st, 280 on the 2nd)
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/bazar", new
        {
            date = new DateOnly(2026, 11, 1),
            items = new[] { new { name = "Rice", quantity = 10m, price = 65m } },
        }, mess));
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/bazar", new
        {
            date = new DateOnly(2026, 11, 2),
            buyerMemberId = ids.MemberAId,
            items = new[] { new { name = "Eggs", quantity = 2m, price = 140m } },
        }, mess));

        // Utilities → 800
        var electricity = (await Body(await manager.SendAsync(Tenant(HttpMethod.Post, "/api/utility-types", new
        {
            name = "Electricity",
        }, mess)))).GetProperty("id").GetGuid();
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/utility", new
        {
            utilityTypeId = electricity, amount = 500m, paymentDate = new DateOnly(2026, 11, 5),
        }, mess));
        var wifi = (await Body(await manager.SendAsync(Tenant(HttpMethod.Post, "/api/utility-types", new
        {
            name = "Wi-Fi",
        }, mess)))).GetProperty("id").GetGuid();
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/utility", new
        {
            utilityTypeId = wifi, amount = 300m, paymentDate = new DateOnly(2026, 11, 6),
        }, mess));

        // Deposits: A pending 100 (EXCLUDED), A approved 400 (manager-recorded), B pending→approved 500,
        // M auto-approved 200 → collected 1100
        await memberA.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            amount = 100m, date = new DateOnly(2026, 11, 10),
        }, mess));
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            memberId = ids.MemberAId, amount = 400m, date = new DateOnly(2026, 11, 11),
        }, mess));
        var bDeposit = await Body(await memberB.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            amount = 500m, date = new DateOnly(2026, 11, 12),
        }, mess)));
        await manager.SendAsync(Tenant(HttpMethod.Put, $"/api/deposits/{bDeposit.GetProperty("id").GetGuid()}/approve", null, mess));
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            amount = 200m, date = new DateOnly(2026, 11, 13),
        }, mess));

        // GET summary (cold — grain rebuild path)
        var summary = await Body(await manager.SendAsync(
            Tenant(HttpMethod.Get, "/api/summary/messes/" + mess + "?year=2026&month=11", null, mess)));

        // §5 hand computation:
        //   rate = 930 / 13.5 = 68.8889 (4dp)   expenditure = 930 + 800 = 1730
        //   onHand = 1100 − 1730 = −630         utilityShare = round(800 / 3, 2) = 266.67
        summary.GetProperty("mealRate").GetDecimal().Should().Be(68.8889m);
        summary.GetProperty("totalMealUnits").GetDecimal().Should().Be(13.5m);
        summary.GetProperty("totalMealCosts").GetDecimal().Should().Be(930m);
        summary.GetProperty("totalUtilityCosts").GetDecimal().Should().Be(800m);
        summary.GetProperty("totalExpenditure").GetDecimal().Should().Be(1730m);
        summary.GetProperty("totalDeposits").GetDecimal().Should().Be(1100m);
        summary.GetProperty("onHandBalance").GetDecimal().Should().Be(-630m);
        summary.GetProperty("locked").GetBoolean().Should().BeFalse();

        var rows = summary.GetProperty("members").EnumerateArray().ToDictionary(r => r.GetProperty("name").GetString());
        rows.Keys.Should().BeEquivalentTo(["Manager", "Member A", "Member B"]);

        // A: units 5.5 → 378.89 + 266.67 = 645.56; deposited 400 (pending 100 excluded) → −245.56
        var a = rows["Member A"]!;
        a.GetProperty("mealUnits").GetDecimal().Should().Be(5.5m);
        a.GetProperty("mealCost").GetDecimal().Should().Be(378.89m);
        a.GetProperty("utilityShare").GetDecimal().Should().Be(266.67m);
        a.GetProperty("totalCost").GetDecimal().Should().Be(645.56m);
        a.GetProperty("deposited").GetDecimal().Should().Be(400m);
        a.GetProperty("balance").GetDecimal().Should().Be(-245.56m);

        // B: units 6 → 413.33 + 266.67 = 680.00; deposited 500 → −180.00
        var b = rows["Member B"]!;
        b.GetProperty("mealCost").GetDecimal().Should().Be(413.33m);
        b.GetProperty("totalCost").GetDecimal().Should().Be(680.00m);
        b.GetProperty("balance").GetDecimal().Should().Be(-180.00m);

        // M: units 2 → 137.78 + 266.67 = 404.45; deposited 200 → −204.45
        var m = rows["Manager"]!;
        m.GetProperty("mealCost").GetDecimal().Should().Be(137.78m);
        m.GetProperty("totalCost").GetDecimal().Should().Be(404.45m);
        m.GetProperty("balance").GetDecimal().Should().Be(-204.45m);

        // Daily trend: 1st = 7 units + 650 bazar; 2nd = 2.5 units + 280 bazar; 3rd = 4 units
        var daily = summary.GetProperty("daily").EnumerateArray().ToList();
        daily.Should().HaveCount(3);
        daily[0].GetProperty("date").GetString().Should().Be("2026-11-01");
        daily[0].GetProperty("mealUnits").GetDecimal().Should().Be(7m);
        daily[0].GetProperty("bazarSpend").GetDecimal().Should().Be(650m);
        daily[1].GetProperty("mealUnits").GetDecimal().Should().Be(2.5m);
        daily[1].GetProperty("bazarSpend").GetDecimal().Should().Be(280m);
        daily[2].GetProperty("mealUnits").GetDecimal().Should().Be(4m);
        daily[2].GetProperty("bazarSpend").GetDecimal().Should().Be(0m);

        // Cache invalidation (P3-BE-4): a cached read followed by a write then a fresh read must
        // reflect the new totals immediately (writes drop the summary cache entry).
        await manager.SendAsync(Tenant(HttpMethod.Get, "/api/summary/messes/" + mess + "?year=2026&month=11", null, mess));
        await manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/bazar", new
        {
            date = new DateOnly(2026, 11, 4),
            items = new[] { new { name = "Fish", quantity = 1m, price = 70m } },
        }, mess));
        var afterWrite = await Body(await manager.SendAsync(
            Tenant(HttpMethod.Get, "/api/summary/messes/" + mess + "?year=2026&month=11", null, mess)));
        afterWrite.GetProperty("totalMealCosts").GetDecimal().Should().Be(1000m);
        afterWrite.GetProperty("onHandBalance").GetDecimal().Should().Be(-700m);

        // Path/header tenant mismatch → 404 (no cross-tenant leakage)
        var mismatch = await manager.SendAsync(
            Tenant(HttpMethod.Get, "/api/summary/messes/" + Guid.NewGuid() + "?year=2026&month=11", null, mess));
        mismatch.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Invalid month → 400
        var badMonth = await manager.SendAsync(
            Tenant(HttpMethod.Get, "/api/summary/messes/" + mess + "?year=2026&month=13", null, mess));
        badMonth.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Manager + two approved members, all with live tokens (summary needs ≥3 actives for the split).</summary>
    private static async Task<(HttpClient Manager, HttpClient A, HttpClient B, Guid MessId, (Guid ManagerId, Guid MemberAId, Guid MemberBId) Ids)> SeedAsync(
        BmmApiFactory factory, string phoneSuffix)
    {
        var (manager, managerToken, _) = await factory.RegisterAsync($"+88017700{phoneSuffix}1", "Manager");
        var (a, aToken, _) = await factory.RegisterAsync($"+88017700{phoneSuffix}2", "Member A");
        var (b, bToken, _) = await factory.RegisterAsync($"+88017700{phoneSuffix}3", "Member B");
        manager.DefaultRequestHeaders.Authorization = new("Bearer", managerToken);
        a.DefaultRequestHeaders.Authorization = new("Bearer", aToken);
        b.DefaultRequestHeaders.Authorization = new("Bearer", bToken);

        var create = await manager.PostAsJsonAsync("/api/messes", new { name = $"Summary Mess {phoneSuffix}" });
        create.EnsureSuccessStatusCode();
        var mess = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        async Task<Guid> ApproveAsync(HttpClient member, string name)
        {
            var invite = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/members/invite")
            {
                Headers = { { "X-Mess-Id", mess.ToString() } },
            });
            invite.EnsureSuccessStatusCode();
            var token = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("inviteToken").GetString()!;
            (await member.PostAsJsonAsync("/api/members/join", new { inviteToken = token })).EnsureSuccessStatusCode();

            var members = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
            {
                Headers = { { "X-Mess-Id", mess.ToString() } },
            });
            var list = await members.Content.ReadFromJsonAsync<JsonElement>();
            var id = list.GetProperty("items").EnumerateArray()
                .Single(x => x.GetProperty("name").GetString() == name).GetProperty("memberId").GetGuid();
            (await manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/members/{id}/approve")
            {
                Headers = { { "X-Mess-Id", mess.ToString() } },
            })).EnsureSuccessStatusCode();
            return id;
        }

        var aId = await ApproveAsync(a, "Member A");
        var bId = await ApproveAsync(b, "Member B");

        var managerList = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", mess.ToString() } },
        });
        var managerId = (await managerList.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("name").GetString() == "Manager").GetProperty("memberId").GetGuid();

        return (manager, a, b, mess, (managerId, aId, bId));
    }
}
