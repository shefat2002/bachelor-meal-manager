# P2-FE-4: Expenses hub — utilities tab + deposits tab

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 frontend session)
- **Phase / track**: Phase 2 / Frontend

## What was built
**Utilities tab**: grid of `UtilityType` cards showing paid/pending status for the active month (derived from that month's `GET /api/expenses/utility` window — no stored status), paid amount + last paid date; inactive types render dimmed. Managers get: `Utility type` dialog (create, 409 on duplicate surfaced via toast), per-card `Record payment` Sheet (type preselected, amount/date/note), and a `Deactivate` soft-disable action.
**Deposits tab**: month-scoped ledger (Member, Date, Note, Amount, status badge: Approved=emerald / Pending=amber / Rejected=rose) with a status filter and approved-month total. Everyone can `Add deposit` (own; members see "pending manager approval"); managers get approve/reject icon buttons on pending rows and can record **on behalf of a member** (member picker → auto-approved server-side).

## Files touched
- `frontend/src/features/expenses/components/utilities-tab.tsx` (cards + create dialog + payment sheet), `components/deposits-tab.tsx` (ledger + deposit sheet).
- `frontend/src/features/expenses/hooks.ts` — type/payment/deposit queries + mutations; all invalidate bazar/deposits/utility-types/summary caches on success.
- `frontend/src/features/expenses/services.ts` — `utilityTypesApi`, `utilityPaymentsApi`, `depositsApi`.

## Key decisions & why
- **Paid/pending derived client-side** from the month's payments — mirrors the P2-BE-4 decision (no status column to drift).
- **Member picker uses `__self` sentinel** in the deposit sheet, stripped before POST — RHF Select needs a concrete default; `undefined` never survives as a selected value.
- Semantic badge classes (`badge-surplus/arrear/pending`) reused for deposit statuses — semantic, not decorative (design-system skill).
- Approve/reject are icon buttons rendered only for managers via `<Authorize>` — backend still enforces (403) regardless.

## How to verify / test
- Manager: add "Electricity" type → card shows Pending → record a payment → card flips to Paid with amount. Member: deposits a contribution → row Pending → manager approves → badge flips + approved total updates.
- `npm run lint && npm run build` green.

## Follow-ups / known gaps
- No edit/delete of utility payments; type deletion (409-guarded) is backend-only — deactivate covers the UI need.
