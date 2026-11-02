# P3-BE-3: GET /api/summary/messes/{messId} — the §5 equations

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 backend session)
- **Phase / track**: Phase 3 / Backend

## What was built
The financial summary endpoint: `GET /api/summary/messes/{messId}?year=&month=` (defaults = current UTC month; any active member may read). `GetMessSummaryQuery` verifies the path messId matches the `X-Mess-Id` tenant (else 404, no leakage), serves the `SnapshotJson` verbatim when the month is locked, and otherwise reads through `ISummaryProvider` (grain + cache). The response carries everything the P3 frontend needs in one fetch: mess totals (rate, units, meal/utility costs, expenditure, deposits, on-hand), per-member settlement rows, and the `daily[]` trend series (date, bazarSpend, mealUnits).

## Files touched
- `backend/src/MealManager.Application/Features/Summary/SummaryContracts.cs` — `MessSummaryDto`, `MemberSettlementRowDto`, `DailyTrendDto`, `MonthLockRequest`.
- `backend/src/MealManager.Application/Features/Summary/Queries/GetMessSummaryQuery.cs` — query + validator + handler.
- `backend/src/MealManager.Api/Controllers/SummaryController.cs`.

## Key decisions & why
- **Math semantics** (Decisions Log): meal rate = bazar ÷ units (utilities shared separately); utility share = equal split per active member (user decision 2026-11-02); collected = **approved deposits only**; rate 4dp, member rows 2dp.
- **Path messId must equal header tenant → 404** — honors both the plan §6.6 route and the "X-Mess-Id is the only tenant selector" rule.
- **`daily[]` inside the summary** — the FE trend chart (P3-FE-2) needs no second endpoint; one cached fetch covers the dashboard.

## How to verify / test
- `SummaryFlowTests.Summary_Matches_Hand_Computed_Values` — the Phase 3 exit criterion: a 3-member seeded month asserted to the cent (rate 68.8889, splits 266.67, balances −245.56/−180.00/−204.45, on-hand −630, pending deposit excluded, daily trend, mismatch → 404, month 13 → 400).
- `PostgresSummaryTests` — rebuild LINQ translates on real PostgreSQL (CI).

## Follow-ups / known gaps
- No per-member "my status" endpoint — the FE picks the caller's row out of `members[]`.
- Utility-share basis is fixed to equal split; a per-mess toggle would need a mess setting (not planned).
