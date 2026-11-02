# P2-FE-2: Meal sheet — monthly matrix grid

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 frontend session)
- **Phase / track**: Phase 2 / Frontend

## What was built
The "Monthly matrix" tab of the meal sheet (desktop spreadsheet view): sticky member-name column on the left, one column per day of the active month, sticky monthly-total column on the right, month-total footer row per day + grand total. Read-only — editing happens in the daily view. Scope follows the top-bar month switcher (Zustand `activeMonth`).

## Files touched
- `frontend/src/features/meals/components/monthly-meal-matrix.tsx` — grid + totals.
- `frontend/src/features/meals/hooks.ts` — `useMonthlyMeals` (key `['messes', messId, 'meals', {month, year}]`, fetches **all pages** of the month range client-side so the ≤100 pageSize cap never truncates a big mess).
- `frontend/src/app/(dashboard)/dashboard/[messId]/meals/page.tsx` — Tabs shell (Daily entry | Monthly matrix).

## Key decisions & why
- **Cells show total units** (B+L+D), not per-slot breakdowns — spreadsheet density; the daily view has the detail.
- **Page-loop fetch in the query fn** — respects API_CONVENTIONS pagination while materializing the whole month for the grid; a `pageSize=100` bump endpoint was deliberately not added.
- Sticky columns via Tailwind `sticky left-0/right-0 bg-background` (opaque background required — `bg-table-header` token doesn't exist in the theme).

## How to verify / test
- Switch months in the top bar → grid re-scopes (query key includes `{month, year}`).
- Cross-check a member's monthly total against the sum of their daily-view totals.

## Follow-ups / known gaps
- Phase 3 settlement table will reuse these totals server-side (grain-computed) — don't sum client-side for money math.
