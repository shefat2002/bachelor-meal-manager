# P0-BE-1: Scaffold .NET 10 solution + /health

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Backend

## What was built

The `backend/` folder went from docs-only to a building, tested .NET 10 solution: six projects in `src/` + two xUnit test projects, with MediatR/FluentValidation wired in Application, EF Core/Npgsql referenced in Infrastructure, health endpoints live on the Api, and a green build + test run.

## Files touched

- `backend/global.json` — SDK pin 10.0.403 (`rollForward: latestFeature`; SDK installed user-local via dotnet-install, this machine had no .NET)
- `backend/MealManager.sln` + `src/`, `tests/` solution folders
- `src/MealManager.Api/Program.cs` — controllers, `AddApplication`/`AddInfrastructure`, `AddServiceDefaults` (P0-BE-2), `/health` + `/health/ready`, `public partial class Program` for tests; template WeatherForecast files removed
- `src/MealManager.Application/DependencyInjection.cs` — `AddApplication()` (MediatR 14.2 + FluentValidation 12.2 from-assembly registration); `Features/README.md` documents the CQRS folder convention
- `src/MealManager.Infrastructure/DependencyInjection.cs` — `AddInfrastructure()` captures `ConnectionStrings:messdb` into `DatabaseOptions` (ApplicationContext registration lands in Phase 1); packages Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 + EF Design
- `src/MealManager.OrleansHost/` — worker placeholder (single log line; silo wiring is Phase 3, in the API process)
- `tests/MealManager.UnitTests/ApplicationScaffoldTests.cs` — resolves `ISender` from `AddApplication()` (needs `AddLogging` — MediatR 14 requires ILoggerFactory)
- `tests/MealManager.IntegrationTests/HealthEndpointTests.cs` — `WebApplicationFactory<Program>`, both health endpoints → 200 "Healthy" (no Docker needed)

## Key decisions & why

- **Explicit `/health` + `/health/ready`** per skill `bmm-observability`, not Aspire's `MapDefaultEndpoints` (`/health`+`/alive`, dev-only) — project convention wins; avoids route ambiguity.
- **OrleansHost scaffolded but unwired** — checklist lists the project; skill `bmm-orleans-grains` says the silo starts in the API process, so this is the future scale-out host.
- **MediatR 14 + FluentValidation 12** installed now so the CQRS skeleton is real from day one.

## How to verify / test

```bash
cd backend && dotnet build && dotnet test   # 3 tests green (1 unit + 2 integration)
```

## Follow-ups / known gaps

- ApplicationContext + first migration (Phase 1); Serilog/Prometheus (Phase 4).
