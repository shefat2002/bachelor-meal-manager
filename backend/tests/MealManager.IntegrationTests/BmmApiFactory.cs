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

/// <summary>In-memory Sqlite-backed API host: full pipeline (auth, tenant middleware, filters) without Docker.</summary>
public sealed class BmmApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtKey = "test-only-jwt-signing-key-0123456789abcdef";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestJwtKey,
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["ConnectionStrings:messdb"] = "DataSource=:memory:",
        }));

        builder.ConfigureServices(services =>
        {
            // EF 9+/10: AddDbContext registers provider config via IDbContextOptionsConfiguration — remove
            // those too, or Npgsql + Sqlite collide in the internal service provider.
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationContext>));
            services.RemoveAll<DbContextOptions<ApplicationContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<ApplicationContext>(options => options.UseSqlite(_connection));

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
        await _connection.DisposeAsync();
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
