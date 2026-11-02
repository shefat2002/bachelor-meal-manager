using System.Text;
using MealManager.Api.Middleware;
using MealManager.Application;
using MealManager.Application.Abstractions;
using MealManager.Grains;
using MealManager.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Orleans;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Orleans silo hosted in the API process (skill bmm-orleans-grains; P3-BE-1). Localhost clustering
// fits the single-silo deployment — ADO.NET-on-Neon clustering is the documented path when silos are
// extracted to the (placeholder) OrleansHost project. Ports are configurable so parallel test hosts
// don't collide on the 11111/30000 defaults.
builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering(
        siloPort: builder.Configuration.GetValue("Orleans:SiloPort", 11111),
        gatewayPort: builder.Configuration.GetValue("Orleans:GatewayPort", 30000),
        serviceId: "bmm",
        clusterId: "bmm");
    // Named provider reserved for a future ADO.NET storage swap; current grain state lives in grain
    // fields with DB-rebuild-on-miss (P3 decision).
    silo.AddMemoryGrainStorage("messState");
});

// Summary read/write ports (cache + grain) — P3-BE-2/3/4.
builder.Services.AddBmmGrainServices();

// HybridCache: L1 in-memory always; L2 Redis distributed cache only when Aspire injected a redis
// connection string (Docker present). Invalidation is push-based via ITotalsProjection.
builder.Services.AddHybridCache();
var redisConnection = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

// JWT auth — key lives in user-secrets (dev) / env (prod); never in appsettings.
// Read config inside the options lambda (resolved lazily on first use) so test-host config
// overrides are honored by BOTH the validator and TokenService.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"] ?? "bmm-api",
            ValidAudience = jwt["Audience"] ?? "bmm-web",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Everything requires auth unless explicitly [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

var allowedOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();

app.MapControllers();
// Liveness = self only ("live" tag, from ServiceDefaults); readiness = all checks incl. Postgres.
app.MapHealthChecks("/health", new() { Predicate = check => check.Tags.Contains("live") }).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

app.Run();

// Partial: gives WebApplicationFactory<Program> access in integration tests.
public partial class Program;
