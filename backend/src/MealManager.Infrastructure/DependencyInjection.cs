using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MealManager.Infrastructure;

public sealed class DatabaseOptions
{
    /// <summary>Neon Postgres pooled connection (DATABASE_URL); injected by the Aspire AppHost or user-secrets.</summary>
    public const string ConnectionStringName = "messdb";

    public string? ConnectionString { get; set; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Configure(o =>
            o.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName));

        // ApplicationContext (EF Core + Npgsql, global MessId query filters) is registered here in Phase 1.
        return services;
    }
}
