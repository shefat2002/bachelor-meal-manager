# P2-FE-1: Meal sheet — daily entry view

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 frontend session)
- **Phase / track**: Phase 2 / Frontend

## What was built
The daily meal sheet at `/dashboard/[messId]/meals` (placeholder replaced in place): one row per active member with `− 0 ½ 1 2 +` steppers for Breakfast/Lunch/Dinner, a per-row total, and day-total footer. Members edit only their own row (others render read-only counts); managers edit all rows. Stepper changes apply optimistically to the TanStack cache and **debounce into one `POST /api/meals` per 700 ms burst**; failures roll back and toast. A `.just-changed` pulse keyframe highlights the touched cell.

## Files touched
- `frontend/src/features/meals/services.ts` — `mealsApi.list/submit`.
- `frontend/src/features/meals/hooks.ts` — `mealKeys` (`['messes', messId, 'meals', {date}]`), `useDailyMeals`, optimistic `useSubmitDailyMeals` (also invalidates the summary key).
- `frontend/src/features/meals/components/meal-stepper.tsx` — `− 0 ½ 1 2 +` control (0–10, 0.5 steps; presets hidden on narrow screens).
- `frontend/src/features/meals/components/daily-meal-sheet.tsx` — date picker, rows, drafts + debounced flush, totals.
- `frontend/src/app/globals.css` — `.just-changed` / `cell-pulse` animation.
- `frontend/src/types/api.ts` (+ `DailyMeal`), `frontend/src/lib/format.ts` (new formatters/month helpers).

## Key decisions & why
- **Guest meals: no separate counter** — counts include guests (P2 backend decision); the sheet shows a hint instead. Members just mark 2.0 lunch when a guest joins.
- **Debounce 700 ms** in-component (draft state per member) — the mutation itself stays a plain optimistic upsert; skill bmm-server-state's "debounce stepper spam" is a UI concern, not an API one.
- **Daily fetch = `from==to` + pageSize 100** — no new `date` query param; conventions §3 stays pure.
- Money/count rendering via `.num` + `formatCount` (no trailing `.0`).

## How to verify / test
- `cd frontend && npm run lint && npm run build` (green).
- Manual: open Meal Sheet as manager (all rows editable) vs member (only own row), tap steppers, watch the pulse + network tab (one POST per burst), reload to confirm persistence.

## Follow-ups / known gaps
- Month-lock (read-only cells when frozen) arrives with P3-BE-5 + P3-FE wiring.
- No B/L/D breakdown in the monthly matrix cells (units only) — by design for density.
