using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MealManager.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace MealManager.IntegrationTests;

/// <summary>
/// P2: proves the Phase 2 domain query filters (DailyMeals/BazarExpenses/Deposits) compile to real
/// PostgreSQL SQL and isolate tenants. Skips without Docker; runs in CI.
/// </summary>
public sealed class PostgresDomainIsolationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    [SkippableFact]
    public async Task Phase2_Domain_Rows_Never_Leak_Across_Messes()
    {
        Skip.IfNot(DockerAvailable(), "Docker is not available — Postgres isolation suite skipped (runs in CI).");

        WebApplicationFactory<Program> factory;
        try
        {
            var postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
            await postgres.StartAsync();
            _postgres = postgres;
            var connectionString = _postgres.GetConnectionString();
            var (siloPort, gatewayPort) = OrleansTestPorts.Get();
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = BmmApiFactory.TestJwtKey,
                    ["ConnectionStrings:messdb"] = connectionString,
                    ["Orleans:SiloPort"] = siloPort.ToString(),
                    ["Orleans:GatewayPort"] = gatewayPort.ToString(),
                }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationContext>));
                    services.RemoveAll<DbContextOptions<ApplicationContext>>();
                    services.RemoveAll<DbContextOptions>();
                    services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connectionString));
                });
            });
        }
        catch (Exception ex) when (ex is DockerUnavailableException || ex.ToString().Contains("DockerUnavailableException"))
        {
            Skip.If(true, "Docker daemon unreachable — Postgres isolation suite skipped (runs in CI).");
            return;
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var alice = factory.CreateClient();
        var aliceToken = await RegisterVia(alice, "+8801760000001", "Alice");
        var bob = factory.CreateClient();
        var bobToken = await RegisterVia(bob, "+8801760000002", "Bob");
        alice.DefaultRequestHeaders.Authorization = new("Bearer", aliceToken);
        bob.DefaultRequestHeaders.Authorization = new("Bearer", bobToken);

        var messA = await CreateMessAsync(alice, "Mess A");
        var messB = await CreateMessAsync(bob, "Mess B");
        var aliceId = await MemberIdAsync(alice, messA, "Alice");
        var bobId = await MemberIdAsync(bob, messB, "Bob");

        var date = new DateOnly(2026, 11, 1);
        foreach (var (client, messId, memberId) in new[] { (alice, messA, aliceId), (bob, messB, bobId) })
        {
            var meal = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/meals")
            {
                Content = JsonContent.Create(new
                {
                    date,
                    entries = new[] { new { memberId, breakfast = 1m, lunch = 1m, dinner = 1m } },
                }),
                Headers = { { "X-Mess-Id", messId } },
            });
            meal.StatusCode.Should().Be(HttpStatusCode.OK);

            var bazar = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
            {
                Content = JsonContent.Create(new
                {
                    date,
                    items = new[] { new { name = "Rice", quantity = 1m, price = 65m } },
                }),
                Headers = { { "X-Mess-Id", messId } },
            });
            bazar.StatusCode.Should().Be(HttpStatusCode.Created);

            var deposit = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/deposits")
            {
                Content = JsonContent.Create(new { amount = 1500m, date }),
                Headers = { { "X-Mess-Id", messId } },
            });
            deposit.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await AssertListOnlyHasOwnRows(alice, messA, aliceId, "/api/meals", "memberId");
        await AssertListOnlyHasOwnRows(alice, messA, aliceId, "/api/expenses/bazar", "buyerMemberId");
        await AssertListOnlyHasOwnRows(alice, messA, aliceId, "/api/deposits", "memberId");

        await factory.DisposeAsync();
    }

    private static async Task AssertListOnlyHasOwnRows(
        HttpClient client, string messId, Guid ownId, string path, string memberProperty)
    {
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{path}?pageSize=100")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
        page.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(row => row.GetProperty(memberProperty).GetGuid() == ownId);
    }

    private static bool DockerAvailable() =>
        File.Exists("/var/run/docker.sock")
        || File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docker/run/docker.sock"));

    private static async Task<string> RegisterVia(HttpClient client, string phone, string name)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { name, phone, password = "Passw0rd!" });
        response.EnsureSuccessStatusCode();
        return await BmmApiFactoryExtensions.ExtractAccessToken(response);
    }

    private static async Task<string> CreateMessAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/messes", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<Guid> MemberIdAsync(HttpClient client, string messId, string name)
    {
        var members = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messId } },
        });
        members.EnsureSuccessStatusCode();
        var list = await members.Content.ReadFromJsonAsync<JsonElement>();
        return list.GetProperty("items").EnumerateArray()
            .Single(m => m.GetProperty("name").GetString() == name)
            .GetProperty("memberId").GetGuid();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;
}
