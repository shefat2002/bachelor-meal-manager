using MealManager.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace MealManager.Api.Middleware;

/// <summary>Readiness probe: opens a real connection to Neon Postgres (pooled messdb string, URI form converted).</summary>
public sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = DependencyInjection.ConvertToNpgsqlConnectionString(
            configuration.GetConnectionString("messdb"));
        if (string.IsNullOrEmpty(connectionString))
        {
            return HealthCheckResult.Unhealthy("messdb connection string is not configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Postgres connection failed.", exception);
        }
    }
}
