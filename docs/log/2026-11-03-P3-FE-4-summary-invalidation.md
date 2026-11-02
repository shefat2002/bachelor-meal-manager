# P3-FE-4: Summary auto-invalidation on money mutations

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 frontend session)
- **Phase / track**: Phase 3 / Frontend

## What was built
Verification + formalization of wiring that was **pre-built in Phase 2**: every money mutation already invalidated the `['messes', messId, 'summary']` prefix (the bmm-server-state "money rule"), and Phase 3's summary query key (`['messes', messId, 'summary', {month, year}]`) sits exactly under that prefix — so TanStack prefix invalidation drops every month's cached summary after any write. This task confirmed the two choke points, documented the key contract in `summaryKeys`, and closed the loop.

## Files touched
- Verified: `frontend/src/features/meals/hooks.ts` (line 111 — `useSubmitDailyMeals.onSettled`) and `frontend/src/features/expenses/hooks.ts` (line 39 — `useInvalidateExpenses`, shared by bazar/utility/deposit/approve mutations).
- Added: `frontend/src/features/summary/hooks.ts` (`summaryKeys.all/detail` so the producer and consumers share one definition instead of string literals).

## Key decisions & why
- **Prefix invalidation over precise keys** — a write can affect the write-month and (via the summary's fresh member query) any cached month; dropping the family is always correct and costs one refetch per visible month.
- The server ALSO invalidates its own HybridCache on every write (P3-BE-4), so client refetches always see fresh grain values — two layers, one rule.

## How to verify / test
- On the overview: log a meal / add bazar / approve a deposit → KPI cards + chart refresh without a manual reload (staleTime 30s is bypassed by invalidation).

## Follow-ups / known gaps
- None for Phase 3; Phase 4's notification stream may add push-based refresh on top.
