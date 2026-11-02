using System.Net.Http.Json;
using System.Text.Json;
using MealManager.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MealManager.IntegrationTests;

/// <summary>
/// In-memory Sqlite-backed API host (full pipeline: auth, tenant middleware, co-hosted Orleans silo).
/// The database uses the shared-cache in-memory pattern: one keeper connection keeps the DB alive
/// while EF pools its own connections — concurrent requests each get a connection, so parallel write
/// tests exercise real interleaving instead of serializing on a single shared connection.
/// </summary>
public sealed class BmmApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtKey = "test-only-jwt-signing-key-0123456789abcdef";

    // Unique per factory instance: parallel test classes never share a database.
    private readonly string _databaseName = $"bmm_test_{Guid.NewGuid():N}";

    private string ConnectionString => $"Data Source=file:{_databaseName}?mode=memory&cache=shared;Default Timeout=30";

    private SqliteConnection? _keeper;

    public async Task InitializeAsync()
    {
        // The keeper connection pins the shared in-memory DB for the whole test class lifetime.
        _keeper = new SqliteConnection(ConnectionString);
        await _keeper.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var (siloPort, gatewayPort) = OrleansTestPorts.Get();
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestJwtKey,
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["ConnectionStrings:messdb"] = ConnectionString,
            ["Orleans:SiloPort"] = siloPort.ToString(),
            ["Orleans:GatewayPort"] = gatewayPort.ToString(),
        }));

        builder.ConfigureServices(services =>
        {
            // EF 9+/10: AddDbContext registers provider config via IDbContextOptionsConfiguration — remove
            // those too, or Npgsql + Sqlite collide in the internal service provider.
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationContext>));
            services.RemoveAll<DbContextOptions<ApplicationContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<ApplicationContext>(options => options.UseSqlite(ConnectionString));

            // The Postgres readiness probe has no real DB in the sqlite environment — drop it.
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var postgres = options.Registrations.FirstOrDefault(r => r.Name == "postgres");
                if (postgres is not null)
                {
                    options.Registrations.Remove(postgres);
                }
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_keeper is not null)
        {
            await _keeper.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}

public static class BmmApiFactoryExtensions
{
    public static async Task<(HttpClient Client, string AccessToken, string RefreshCookie)> RegisterAsync(
        this BmmApiFactory factory, string phone, string name = "Test User", string password = "Passw0rd!")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { name, phone, password });
        response.EnsureSuccessStatusCode();
        return (client, await ExtractAccessToken(response), ExtractRefreshCookie(response));
    }

    public static async Task<string> ExtractAccessToken(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bmm_refresh="));
        return setCookie.Split(';')[0]; // "bmm_refresh=<token>"
    }
}
