# P3-BE-5: Month lock — freeze edits after settlement

- **Date**: 2026-11-03
- **Agent**: ZCode (Phase 3 backend session)
- **Phase / track**: Phase 3 / Backend

## What was built
Month freeze with durable snapshots. New `MonthLock` entity (unique `(MessId, Year, Month)`; **row existence = locked**; `SnapshotJson` holds the serialized `MessSummaryDto` captured at lock time) + migration `Phase3_Month_Locks` (applied to Neon). A shared `MonthLockGuard.RequireOpenAsync` throws **409 CONFLICT** on every write dated in a locked month: meal upserts, bazar creates, utility payments, deposit creates, and deposit *approval* (rejecting stays allowed — it moves no money). `PUT /api/summary/messes/{messId}/month-lock {year, month, locked}` (manager-only) locks (fresh computation → snapshot persisted → grain evicts + cache drops) or unlocks (row deleted → live recompute resumes). While locked, `GET summary` serves the snapshot with `locked: true`.

## Files touched
- `backend/src/MealManager.Application/Domain/MonthLock.cs`, `Common/MonthLockGuard.cs`.
- `Infrastructure/ApplicationContext.cs` (+ `IApplicationContext`) — config, query filter, unique index; migration `20261102182919_Phase3_Month_Locks`.
- Guards + wiring in: `SubmitDailyMealsCommand`, `CreateBazarExpensesCommand`, `CreateUtilityPaymentCommand`, `CreateDepositCommand`, `ApproveRejectDepositCommands`.
- `Features/Summary/Commands/SetMonthLockCommand.cs` + `SummaryController` PUT route.

## Key decisions & why
- **Row-existence semantics + JSON snapshot** — locked months are immutable and served without touching grains (skill: "closed months live in the DB"); unlock simply deletes the row and lets the rebuild path take over.
- **Deposit approval is frozen too** — approving moves money INTO the month's collected total; a frozen month must not move money. Rejection stays open (removes nothing from totals).
- **Lock recomputes fresh** (invalidate → get) so the snapshot can't be a stale cached value.
- Re-locking an existing row overwrites its snapshot (idempotent).

## How to verify / test
- `MonthLockTests`: member lock → 403; month 13 → 400; lock → all five write paths + approval → 409; December unaffected; snapshot values == pre-lock; unlock → approval succeeds and live summary reflects it (`totalDeposits` 300).
- Migration applied to Neon production branch via the swap-apply-restore flow.

## Follow-ups / known gaps
- No audit trail of who locked (LockedBy exists on the row; not exposed in the API yet) — expose when the FE asks.
- PDF export of the snapshot is P4-BE-3; the FE export button (P3-FE-3) prints client-side meanwhile.
