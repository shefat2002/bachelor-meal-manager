using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

// Postgres is Neon (cloud) — never a local container. Two connection strings:
//   messdb            → Neon pooled URL (DATABASE_URL)          — app runtime via Npgsql
//   messdb-migrations → Neon direct URL (DATABASE_URL_UNPOOLED) — dotnet ef migrations (DDL over PgBouncer fails)
// Values live in AppHost user-secrets (see backend/README.md).
var messdb = builder.AddConnectionString("messdb");
var messdbMigrations = builder.AddConnectionString("messdb-migrations");

var api = builder.AddProject<Projects.MealManager_Api>("api")
    .WithHttpEndpoint(port: 5080)
    .WithReference(messdb)
    .WithReference(messdbMigrations);

builder.AddProject<Projects.MealManager_OrleansHost>("orleanshost");

// Redis/RabbitMQ are Docker containers. When Docker isn't available the AppHost still
// runs (api + orleanshost + frontend); skip them with a warning instead of failing.
// Wire-up of Redis/RabbitMQ clients happens in Phase 3/4, so nothing breaks without them today.
if (IsDockerAvailable())
{
    var redis = builder.AddRedis("redis");
    var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();
    api.WithReference(redis).WithReference(rabbitmq);
}
else
{
    Console.WriteLine("[AppHost] Docker not available — skipping redis/rabbitmq container resources.");
}

builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();

static bool IsDockerAvailable()
{
    try
    {
        var startInfo = new ProcessStartInfo("docker", "info")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        return process.WaitForExit(10_000) && process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}
