using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>
/// P3-BE-2 regression test — the reason Orleans exists: hammer one mess with concurrent writes and
/// assert the summary equals the exact sum (Phase 3 exit criterion).
/// </summary>
public sealed class ConcurrentWritesTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    [Fact]
    public async Task Concurrent_Writes_Never_Produce_A_Wrong_Total()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "51");
        var (client, messId) = (mess.Manager, mess.MessId);

        // 20 deterministic meal posts: 2 members × 10 days × 2.5 units (b1 + l1 + d0.5) = 50 units.
        var mealPosts = Enumerable.Range(1, 10).SelectMany(day => new[]
        {
            client.SendAsync(MealPost(messId, mess.ManagerId, day)),
            client.SendAsync(MealPost(messId, mess.MemberId, day)),
        });

        // 5 bazar posts × 100 = 500; 5 pending deposits × 200 approved afterwards = 1000.
        var bazarPosts = Enumerable.Range(1, 5).Select(day => client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, day),
                items = new[] { new { name = $"Item {day}", quantity = 1m, price = 100m } },
            }),
            Headers = { { "X-Mess-Id", messId.ToString() } },
        }));

        var depositPosts = Enumerable.Range(1, 5).Select(day => mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
        {
            Content = JsonContent.Create(new { amount = 200m, date = new DateOnly(2026, 11, day) }),
            Headers = { { "X-Mess-Id", messId.ToString() } },
        }));

        var responses = await Task.WhenAll(mealPosts.Concat(bazarPosts).Concat(depositPosts));
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);

        // Approve all five pending deposits (their ids come from the ledger).
        var ledger2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/deposits?status=Pending&pageSize=50")
        {
            Headers = { { "X-Mess-Id", messId.ToString() } },
        });
        var pending = (await ledger2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToList();
        pending.Should().HaveCount(5);
        foreach (var deposit in pending)
        {
            (await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/deposits/{deposit.GetProperty("id").GetGuid()}/approve")
            {
                Headers = { { "X-Mess-Id", messId.ToString() } },
            })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // Every write (and its cache invalidation) completed before these responses returned →
        // the final summary must be exact.
        var summary = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/summary/messes/" + messId + "?year=2026&month=11")
        {
            Headers = { { "X-Mess-Id", messId.ToString() } },
        });
        var body = await summary.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("totalMealUnits").GetDecimal().Should().Be(50m);
        body.GetProperty("totalMealCosts").GetDecimal().Should().Be(500m);
        body.GetProperty("totalDeposits").GetDecimal().Should().Be(1000m);
        body.GetProperty("mealRate").GetDecimal().Should().Be(10m); // 500 / 50
        body.GetProperty("onHandBalance").GetDecimal().Should().Be(500m); // 1000 − 500 − 0

        var rows = body.GetProperty("members").EnumerateArray().ToDictionary(r => r.GetProperty("name").GetString());
        rows.Count.Should().Be(2);
        foreach (var row in rows.Values)
        {
            row.GetProperty("mealUnits").GetDecimal().Should().Be(25m);
            row.GetProperty("deposited").GetDecimal().Should().Be(row.GetProperty("name").GetString() == "Member Guy" ? 1000m : 0m);
        }

        static HttpRequestMessage MealPost(Guid messId, Guid memberId, int day) => new(HttpMethod.Post, "/api/meals")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, day),
                entries = new[] { new { memberId, breakfast = 1m, lunch = 1m, dinner = 0.5m } },
            }),
            Headers = { { "X-Mess-Id", messId.ToString() } },
        };
    }
}
