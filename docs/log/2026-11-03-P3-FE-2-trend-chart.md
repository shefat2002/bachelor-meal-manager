# P3-FE-2: Recharts — daily bazar spend vs meal volume

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 frontend session)
- **Phase / track**: Phase 3 / Frontend

## What was built
The trend chart on the overview: a ComposedChart with **bazar spend as indigo bars** (left axis, `--chart-1`) and **meal units as a line** (right axis, `--chart-2`), day-of-month on the X axis, tooltips formatting money vs units, and a friendly empty state when the month has no activity. Built on the shadcn `chart` wrapper (`npx shadcn add chart`), which vendors `chart.tsx` and installed **recharts 3.8**.

## Files touched
- `frontend/src/features/summary/components/trend-chart.tsx`.
- `frontend/src/components/ui/chart.tsx` (shadcn-generated), `frontend/package.json` (+`recharts`).

## Key decisions & why
- **shadcn chart wrapper over raw recharts** — it maps config keys to CSS vars (`--color-*`) so the chart follows light/dark tokens; raw SVG `fill="var(--x)"` attributes don't resolve `var()` reliably.
- **Dual axes** (money vs units have different scales); numbers use `.num` classes on axes per the design system.
- Data comes from `summary.daily[]` (already in the one cached summary fetch) — no dedicated trend endpoint, no client aggregation.

## How to verify / test
- Log bazar on a day with meals → bar + line point appear for that day; hover shows "Day N" with spend + units. Empty month → guidance message, no broken axis.

## Follow-ups / known gaps
- 31 days render fine at desktop width; mobile relies on horizontal squeeze (bars thin out) — acceptable for now.
