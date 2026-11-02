# P3-BE-2: MessGrain + MemberGrain — running totals, sequential concurrency

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 backend session)
- **Phase / track**: Phase 3 / Backend

## What was built
The two grains prescribed by skill `bmm-orleans-grains`. **`MessGrain`** (key = messId) holds per-month aggregates (total units, bazar cost, utility cost, approved deposits, per-day units+bazar) and orchestrates summaries: computes the meal rate/on-hand, queries active members fresh (equal split must reflect current membership), and fans out to **`MemberGrain`** (compound key memberId+messId) which owns each member's monthly standing and performs the member-level §5 math via the shared `SettlementMath`. Write paths reach grains through the `ITotalsProjection` port (Application stays Orleans-free; `[GenerateSerializer]` transport records live in Grains). Rebuild-on-miss queries the domain tables directly (scoped `IApplicationContext` per turn).

## Files touched
- `backend/src/MealManager.Grains/` — `Contracts/GrainContracts.cs` (transport records), `SettlementMath.cs`, `IMessGrain.cs`/`MessGrain.cs`, `IMemberGrain.cs`/`MemberGrain.cs`, `Projections/TotalsProjection.cs`, `Projections/SummaryProvider.cs`, `DependencyInjection.cs`.
- `backend/src/MealManager.Application/Abstractions/` — new ports `ITotalsProjection.cs` (+ `MealChange` record), `ISummaryProvider.cs`.
- Write handlers wired to the projection: meals (unit old→new deltas), bazar (raw Σ qty×price — matches rebuild math), utility payments, deposits (approved-only), deposit approval.

## Key decisions & why
- **Deltas apply only to RESIDENT state** — the projection runs AFTER SaveChanges, so a rebuild already contains the write; applying a delta on top would double-count. Evicted state rebuilds correctly on demand. This is the load-bearing correctness rule of the whole design.
- **Bazar increments are unrounded** (`Σ quantity × price`, no per-line rounding) so grain increments are bit-identical to a DB rebuild (the DTO keeps the rounded 2dp display value).
- **Post-commit projection failures are logged + swallowed** — a committed write must never 500 because of a cache/grain hiccup; rebuild self-heals.
- **Newest-3-months eviction** in both grains (state size guard per the skill); summary queries active members fresh instead of caching the member list in state.
- Rebuild LINQ is provider-agnostic (works on sqlite + Npgsql — proven by `PostgresSummaryTests`).

## How to verify / test
- `ConcurrentWritesTests`: 30 parallel writes (meals/bazar/deposits + approvals) then summary must equal exact sums — the Orleans regression test, green.
- `SettlementMathTests` (unit) — equations + zero-unit/zero-member guards.
- Summary math e2e in `SummaryFlowTests` (hand-computed §5 dataset).

## Follow-ups / known gaps
- Membership changes (join/leave mid-month) are picked up on the next summary (fresh member query) — no event needed.
- No grain unit-test kit (Orleans TestingHost) — integration tests cover the behavior; add later if grain logic grows.
