using MealManager.Application;
using MealManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
// Liveness: process is up. Readiness will grow dependency checks (Postgres, Redis, RabbitMQ) in later phases.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();

// Partial: gives WebApplicationFactory<Program> access in integration tests.
public partial class Program;
