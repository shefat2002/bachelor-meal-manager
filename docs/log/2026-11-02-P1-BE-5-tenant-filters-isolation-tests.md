# P1-BE-5: Global MessId query filters + tenant-isolation tests

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Backend

## What was built

The isolation backbone: `TenantResolutionBehavior` (MediatR pipeline, after Validation) resolves `X-Mess-Id` + active membership before any handler and populates the scoped `TenantContext`; `ApplicationContext` applies `HasQueryFilter(e => e.MessId == _tenant.MessId)` to `MemberMess` and `InviteLink`. Three test layers: 10 unit (validators), 12 sqlite-in-memory e2e (full HTTP pipeline, runs everywhere), 1 TestContainers **Postgres** isolation test (`[SkippableFact]` — real Npgsql SQL, runs in CI, skips without Docker).

## Files touched

- `Application/Behaviors/{ValidationBehavior,TenantResolutionBehavior}.cs`; `Application/DependencyInjection.cs` (behavior order)
- `Infrastructure/ApplicationContext.cs` (filters), `MembershipService.cs`, `DependencyInjection.cs` (TenantContext concrete + interface)
- `tests/MealManager.IntegrationTests/{BmmApiFactory,AuthFlowTests,MessMemberFlowTests,RefreshRotationThroughHostTests,PostgresTenantIsolationTests,HealthEndpointTests}.cs`; `tests/MealManager.UnitTests/ValidatorTests.cs`

## Key decisions & why

- **Filter + explicit escape hatch**: cross-mess queries (my-messes list, membership checks, join) use `IgnoreQueryFilters()` deliberately; everything in-mess is filtered by construction.
- **EF10 test-host gotchas**: provider swap requires removing `IDbContextOptionsConfiguration<T>` (not just `DbContextOptions`); the Postgres readiness check is dropped in the sqlite factory; **each cookie-carrying HTTP stage needs a FRESH HttpClient** (the test client caches cookies per instance — a reused client made replay-rotation look like 200).
- **TestContainers skip logic**: container Build()/Start() inside the test with `Skip.If` on `DockerUnavailableException` (constructing at field-init throws before the skip runs).

## How to verify / test

```bash
cd backend && dotnet test   # 10 unit + 12 integration pass, 1 PG isolation skipped locally (green in CI)
```

## Follow-ups / known gaps

- The PG isolation suite runs in CI on push/PR — watch it on the first branch build.
