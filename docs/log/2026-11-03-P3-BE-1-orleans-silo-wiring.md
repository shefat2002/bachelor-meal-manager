# P3-BE-1: Orleans silo wiring in the API host

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 backend session)
- **Phase / track**: Phase 3 / Backend

## What was built
The Orleans **10.4.2** silo is co-hosted in the API process (`builder.Host.UseOrleans` in Program.cs) with localhost clustering (service/cluster id `bmm`, ports from `Orleans:SiloPort`/`Orleans:GatewayPort`, defaults 11111/30000) and a named `messState` memory grain-storage provider reserved for a future ADO.NET-on-Neon swap. New project `MealManager.Grains` (Orleans.Sdk codegen + `Microsoft.Extensions.Caching.Hybrid`), referenced by Api; the OrleansHost project stays the placeholder for silo extraction. All test factories (`BmmApiFactory`, both Postgres suites) inject **random free ports** so parallel co-hosted silos don't collide.

## Files touched
- `backend/src/MealManager.Grains/MealManager.Grains.csproj` — new project (Orleans.Sdk 10.4.2, Caching.Hybrid 10.11.0, ref Application); added to `MealManager.slnx`.
- `backend/src/MealManager.Api/Program.cs` — UseOrleans + `AddBmmGrainServices()` + `AddHybridCache()` + conditional Redis L2 (`AddStackExchangeRedisCache` when `ConnectionStrings:redis` exists).
- `backend/src/MealManager.Api/MealManager.Api.csproj` — Orleans.Server 10.4.2, Caching.StackExchangeRedis 10.0.14.
- Tests: `OrleansTestPorts.cs` (free-port probe), port overrides in `BmmApiFactory`/`PostgresTenantIsolationTests`/`PostgresDomainIsolationTests`.

## Key decisions & why
- **Localhost clustering, not Redis/ADO.NET tables** — single-silo deployment can't use cluster tables; ADO.NET-on-Neon is the documented multi-silo path (logged in Decisions Log). User decision 2026-11-02.
- **No Redis grain storage** — no first-party Orleans Redis provider exists; memory + DB-rebuild covers durability (DB is the record).
- **.NET 10 API renames**: `AddStackExchangeRedisCache` (was `AddStackExchangeRedisDistributedCache`); Orleans went 10.x to align with .NET 10.
- **AppHost unchanged** — it already injects `ConnectionStrings:redis` into the API only when Docker exists; the cache degrades to L1-only without it.

## How to verify / test
- `cd backend && dotnet test` — full Phase 1+2 suite stays green with the silo co-hosted (29 unit + 33 integration).
- AppHost run: silo logs appear alongside API startup; no Redis needed.

## Follow-ups / known gaps
- Orleans dashboard intentionally not added (Phase 4 observability may take it).
- Silo extraction to OrleansHost = switch clustering to ADO.NET-on-Neon + storage swap; grain code unchanged.
