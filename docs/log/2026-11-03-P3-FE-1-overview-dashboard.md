# P3-FE-1: Overview dashboard — KPI cards, today snapshot, recent activity

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 frontend session)
- **Phase / track**: Phase 3 / Frontend

## What was built
The overview page (`/dashboard/[messId]`) graduated from its placeholder: four KPI cards straight from the §5 summary (meal rate with units/bazar subtext, on-hand balance colored surplus/arrear, total expenditure with bazar+utility breakdown, **my status** = the caller's settlement balance with Surplus/Arrear/Settled wording), a "Today" snapshot (total meals + how many members are eating, from the existing daily-meals query), and a merged bazar/deposit activity feed (top 6 by recency). Everything is scoped by the top-bar month switcher (`activeMonth`); a `Locked` badge appears when the month is frozen.

## Files touched
- `frontend/src/app/(dashboard)/dashboard/[messId]/page.tsx` — full implementation.
- `frontend/src/types/api.ts` — `MessSummary`, `MemberSettlementRow`, `DailyTrend` (mirror `MessSummaryDto`).
- `frontend/src/features/summary/services.ts` + `hooks.ts` (new feature dir; key `['messes', messId, 'summary', {month, year}]`).

## Key decisions & why
- **My status = the caller's row from `members[]`** — no dedicated per-member endpoint; the summary already carries everyone.
- **Activity feed from existing hooks** (bazar + deposits, page 1, month-scoped) — no new backend surface.
- KPI values render `.num` (mono tabular-nums) per the design system; on-hand/my-status use the semantic surplus/arrear tokens only for money meaning.

## How to verify / test
- `npm run lint && npm run build` green. Manual: log meals/expenses/deposits in the current month → KPIs update after each write (client invalidation) and match the reports page.

## Follow-ups / known gaps
- Quick-entry button + notification popover are Phase 4 items on this same page.
