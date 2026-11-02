# P3-FE-3: Master settlement table + export/close bar

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 frontend session)
- **Phase / track**: Phase 3 / Frontend

## What was built
The reports page (`/dashboard/[messId]/reports`): the master settlement table — one row per active member with Meals, Meal cost, Utility share, Total cost, Deposited and **Balance** (emerald surplus / rose arrear badge), plus a mess-total footer — and the export/close bar: **Export PDF** (browser print with `print:hidden` chrome stripping; server-generated PDF is P4-BE-3) and the manager-gated **Lock/Unlock month** toggle (confirm dialog → `PUT /api/summary/messes/{messId}/month-lock`; the badge flips to `Locked` and the backend 409s all writes dated in the month).

## Files touched
- `frontend/src/app/(dashboard)/dashboard/[messId]/reports/page.tsx` — placeholder replaced.
- `frontend/src/features/summary/hooks.ts` — `useSetMonthLock` (invalidates the whole summary family on success).

## Key decisions & why
- **Rows are rendered verbatim from the server summary** — footer totals sum the *displayed rows* for consistency; no §5 math is derived on the client (root AGENTS.md §4).
- **Lock is confirm-gated** — it freezes every write dated in the month; unlocking recomputes live, so both directions warn.
- Print-based export now (zero deps, works offline); the real PDF pipeline (P4-BE-3) will replace the button's backend call, not the table.

## How to verify / test
- Seed a month → row math matches the overview KPIs. Lock as manager → badge + write attempts anywhere (meal sheet, expenses hub) fail with 409 toasts; unlock → edits resume.

## Follow-ups / known gaps
- No per-column sorting (server rows arrive name-ordered) — add if users ask.
