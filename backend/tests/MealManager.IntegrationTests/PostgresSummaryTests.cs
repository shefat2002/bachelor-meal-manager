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
/// P3: proves the grain rebuild LINQ (GroupBy sums, decimal aggregates, Quantity × Price) translates
/// to real PostgreSQL SQL. Skips without Docker; runs in CI.
/// </summary>
public sealed class PostgresSummaryTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    [SkippableFact]
    public async Task Summary_Rebuild_Translates_On_Postgres()
    {
        Skip.IfNot(DockerAvailable(), "Docker is not available — Postgres summary suite skipped (runs in CI).");

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
            Skip.If(true, "Docker daemon unreachable — Postgres summary suite skipped (runs in CI).");
            return;
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var manager = factory.CreateClient();
        var token = await RegisterVia(manager, "+8801780000001", "Manager");
        manager.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var create = await manager.PostAsJsonAsync("/api/messes", new { name = "PG Summary Mess" });
        create.EnsureSuccessStatusCode();
        var mess = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var managerId = await MemberIdAsync(manager, mess, "Manager");

        (await manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/meals")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 1),
                entries = new[] { new { memberId = managerId, breakfast = 1m, lunch = 1m, dinner = 0.5m } },
            }),
            Headers = { { "X-Mess-Id", mess.ToString() } },
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/bazar")
        {
            Content = JsonContent.Create(new
            {
                date = new DateOnly(2026, 11, 1),
                items = new[] { new { name = "Rice", quantity = 3m, price = 50m } },
            }),
            Headers = { { "X-Mess-Id", mess.ToString() } },
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await manager.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/summary/messes/{mess}?year=2026&month=11")
        {
            Headers = { { "X-Mess-Id", mess.ToString() } },
        });
        summary.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await summary.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalMealUnits").GetDecimal().Should().Be(2.5m);
        body.GetProperty("totalMealCosts").GetDecimal().Should().Be(150m);
        body.GetProperty("mealRate").GetDecimal().Should().Be(60m); // 150 / 2.5

        await factory.DisposeAsync();
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

    private static async Task<Guid> MemberIdAsync(HttpClient client, Guid messId, string name)
    {
        var members = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messId.ToString() } },
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
