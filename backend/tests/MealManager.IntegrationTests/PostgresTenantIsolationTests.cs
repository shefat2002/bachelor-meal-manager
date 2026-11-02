using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MealManager.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace MealManager.IntegrationTests;

/// <summary>
/// P1-BE-5: proves the MessId global query filters compile to real PostgreSQL SQL and isolate tenants.
/// Runs wherever Docker is available (CI); auto-skips on machines without Docker.
/// </summary>
public sealed class PostgresTenantIsolationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    [SkippableFact]
    public async Task Members_Of_MessA_Never_Return_MessB_Rows()
    {
        Skip.IfNot(DockerAvailable(), "Docker is not available — Postgres isolation suite skipped (runs in CI).");

        PostgreSqlContainer postgres;
        try
        {
            // Testcontainers 4.x resolves the Docker endpoint at Build() time — construct inside the try.
            postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
            await postgres.StartAsync();
        }
        catch (Exception ex) when (ex is DockerUnavailableException || ex.ToString().Contains("DockerUnavailableException"))
        {
            Skip.If(true, "Docker daemon unreachable — Postgres isolation suite skipped (runs in CI).");
            return;
        }

        _postgres = postgres;
        var connectionString = _postgres.GetConnectionString();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = BmmApiFactory.TestJwtKey,
                ["ConnectionStrings:messdb"] = connectionString,
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationContext>>();
                services.RemoveAll<DbContextOptions>();
                services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connectionString));
            });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var alice = factory.CreateClient();
        var aliceToken = await RegisterVia(alice, "+8801730000001", "Alice");
        var bob = factory.CreateClient();
        var bobToken = await RegisterVia(bob, "+8801730000002", "Bob");
        alice.DefaultRequestHeaders.Authorization = new("Bearer", aliceToken);
        bob.DefaultRequestHeaders.Authorization = new("Bearer", bobToken);

        var messA = await CreateMessAsync(alice, "Mess A");
        var messB = await CreateMessAsync(bob, "Mess B");

        // Bob also joins Mess A, so the members table holds rows for both tenants
        var inviteA = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/members/invite")
        {
            Headers = { { "X-Mess-Id", messA } },
        });
        inviteA.EnsureSuccessStatusCode();
        var tokenA = (await inviteA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("inviteToken").GetString()!;
        await bob.PostAsJsonAsync("/api/members/join", new { inviteToken = tokenA });

        // Sanity straight from the DB: both messes have membership rows
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            (await db.MembersMesses.IgnoreQueryFilters().CountAsync()).Should().BeGreaterThanOrEqualTo(3);
        }

        // Alice listing members of Mess A with X-Mess-Id: only Mess A rows — global filter scoped by tenant
        var membersOfA = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messA } },
        });
        membersOfA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await membersOfA.Content.ReadFromJsonAsync<JsonElement>();
        var namesInMessA = bodyA.GetProperty("items").EnumerateArray()
            .Select(m => m.GetProperty("name").GetString())
            .ToList();
        namesInMessA.Should().Contain("Alice").And.Contain("Bob");
        namesInMessA.Should().HaveCount(2); // no Mess-B-only rows ever leak

        // Alice cannot list members of Mess B (non-member → 403, filters never leak rows)
        var asMessB = await alice.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/members")
        {
            Headers = { { "X-Mess-Id", messB } },
        });
        asMessB.StatusCode.Should().Be(HttpStatusCode.Forbidden);

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

    private static async Task<string> CreateMessAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/messes", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;
}
