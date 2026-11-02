using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>
/// P3-BE-5: month lock freezes every write dated in the month (409 CONFLICT), serves the snapshot
/// captured at lock time, and unlock resumes live edits.
/// </summary>
public sealed class MonthLockTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static HttpRequestMessage Tenant(HttpMethod method, string uri, object? payload, Guid messId)
    {
        var message = new HttpRequestMessage(method, uri) { Headers = { { "X-Mess-Id", messId.ToString() } } };
        if (payload is not null)
        {
            message.Content = JsonContent.Create(payload);
        }

        return message;
    }

    [Fact]
    public async Task Lock_Freezes_Writes_Serves_Snapshot_And_Unlock_Resumes()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "61");

        // Seed one pending deposit (approved only after the lock attempt) + some meals
        await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 11, 1),
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1m, dinner = 1m } },
        }, mess.MessId));
        var pending = await Body(await mess.Member.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            amount = 300m, date = new DateOnly(2026, 11, 15),
        }, mess.MessId)));
        var pendingId = pending.GetProperty("id").GetGuid();

        var before = await Body(await mess.Manager.SendAsync(
            Tenant(HttpMethod.Get, $"/api/summary/messes/{mess.MessId}?year=2026&month=11", null, mess.MessId)));

        // Member cannot lock; bad month fails validation
        (await mess.Member.SendAsync(Tenant(HttpMethod.Put, $"/api/summary/messes/{mess.MessId}/month-lock",
            new { year = 2026, month = 11, locked = true }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Put, $"/api/summary/messes/{mess.MessId}/month-lock",
            new { year = 2026, month = 13, locked = true }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Manager locks November
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Put, $"/api/summary/messes/{mess.MessId}/month-lock",
            new { year = 2026, month = 11, locked = true }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Every write path dated in November → 409 CONFLICT
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 11, 2),
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1m, dinner = 1m } },
        }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/bazar", new
        {
            date = new DateOnly(2026, 11, 2),
            items = new[] { new { name = "Rice", quantity = 1m, price = 65m } },
        }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var typeResponse = await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/utility-types", new { name = "Gas" }, mess.MessId));
        var typeId = (await Body(typeResponse)).GetProperty("id").GetGuid();
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/expenses/utility", new
        {
            utilityTypeId = typeId, amount = 100m, paymentDate = new DateOnly(2026, 11, 20),
        }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await mess.Member.SendAsync(Tenant(HttpMethod.Post, "/api/deposits", new
        {
            amount = 100m, date = new DateOnly(2026, 11, 16),
        }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Approving a deposit DATED in the frozen month also freezes
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Put, $"/api/deposits/{pendingId}/approve", null, mess.MessId)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // December stays open
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Post, "/api/meals", new
        {
            date = new DateOnly(2026, 12, 1),
            entries = new[] { new { memberId = mess.ManagerId, breakfast = 1m, lunch = 1m, dinner = 1m } },
        }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Snapshot served while locked — identical values, locked=true
        var frozen = await Body(await mess.Member.SendAsync(
            Tenant(HttpMethod.Get, $"/api/summary/messes/{mess.MessId}?year=2026&month=11", null, mess.MessId)));
        frozen.GetProperty("locked").GetBoolean().Should().BeTrue();
        frozen.GetProperty("totalMealUnits").GetDecimal().Should().Be(before.GetProperty("totalMealUnits").GetDecimal());
        frozen.GetProperty("totalDeposits").GetDecimal().Should().Be(before.GetProperty("totalDeposits").GetDecimal());

        // Unlock → November writes resume and the summary recomputes live
        (await mess.Manager.SendAsync(Tenant(HttpMethod.Put, $"/api/summary/messes/{mess.MessId}/month-lock",
            new { year = 2026, month = 11, locked = false }, mess.MessId))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await mess.Manager.SendAsync(Tenant(HttpMethod.Put, $"/api/deposits/{pendingId}/approve", null, mess.MessId)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var live = await Body(await mess.Manager.SendAsync(
            Tenant(HttpMethod.Get, $"/api/summary/messes/{mess.MessId}?year=2026&month=11", null, mess.MessId)));
        live.GetProperty("locked").GetBoolean().Should().BeFalse();
        live.GetProperty("totalDeposits").GetDecimal().Should().Be(300m);
    }
}
