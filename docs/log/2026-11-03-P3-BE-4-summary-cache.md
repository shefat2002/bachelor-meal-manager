# P3-BE-4: HybridCache for summary + write-driven invalidation

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 backend session)
- **Phase / track**: Phase 3 / Backend

## What was built
`SummaryProvider` wraps the grain read in **HybridCache** (`summary:{messId}:{year}-{month}`, 15-min safety expiry, tag `summary:{messId}`). L1 (in-memory) always; L2 Redis distributed cache registers **only when Aspire injected `ConnectionStrings:redis`** (Docker present) — locally and in CI the cache is L1-only, so no infra is required to develop or test. Invalidation is push-based and lives in ONE place: `TotalsProjection` removes the cache entry after forwarding every write delta (meals/bazar/utility/approved deposits/month lock) — the single choke point the money rule needs.

## Files touched
- `backend/src/MealManager.Grains/Projections/SummaryProvider.cs` — cache-aside + invalidate.
- `backend/src/MealManager.Grains/Projections/TotalsProjection.cs` — cache removal per write.
- `backend/src/MealManager.Api/Program.cs` — `AddHybridCache()` + conditional `AddStackExchangeRedisCache`.

## Key decisions & why
- **HybridCache over raw IDistributedCache** — free L1/L2 split, stampede protection, graceful L1-only fallback; no Redis health check added (kept the package surface minimal — Phase 4 observability can add it).
- **.NET 10 rename**: `AddStackExchangeRedisDistributedCache` → **`AddStackExchangeRedisCache`** (the old name no longer compiles).
- **Redis path is config-identical** to L1 (same StackExchange call), so no dedicated Redis test container — correctness of invalidation is provider-independent and covered by the sqlite suite.

## How to verify / test
- `SummaryFlowTests` cache section: GET (warm) → POST bazar → GET reflects new totals immediately (invalidation works through the cache).
- With Docker + Aspire: redis container injects the connection string and L2 activates automatically.

## Follow-ups / known gaps
- Tag-based bulk invalidation (`RemoveByTagAsync`) available if a future write ever spans months; not needed yet.
