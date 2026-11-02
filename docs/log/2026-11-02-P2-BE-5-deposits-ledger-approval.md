# P2-BE-5: Deposits — ledger + manager approval

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 backend session)
- **Phase / track**: Phase 2 / Backend

## What was built
Member deposit ledger with approval workflow: `POST /api/deposits` (member → own deposit, starts `Pending`; **manager → any active member, auto-`Approved`** with `ApprovedBy` = creator — user decision), `GET /api/deposits` (filters `memberId`/`status`/`from`/`to`, sorts `date`|`amount`, paginated, joins member name), `PUT /api/deposits/{id}/approve | reject` (manager-only, **only `Pending` deposits transition — anything else 409**). Members cannot deposit for anyone else (403).

## Files touched
- `backend/src/MealManager.Application/Domain/Deposit.cs` — new entity + `DepositStatus {Pending, Approved, Rejected}` (stored as string).
- `backend/src/MealManager.Application/Features/Deposits/DepositContracts.cs` — DTO + request record.
- `backend/src/MealManager.Application/Features/Deposits/Commands/CreateDepositCommand.cs`.
- `backend/src/MealManager.Application/Features/Deposits/Commands/ApproveRejectDepositCommands.cs` — shared `DepositReview.SetStatusAsync` (mirrors `ApproveRejectMemberCommands` pattern).
- `backend/src/MealManager.Application/Features/Deposits/Queries/GetDepositsQuery.cs`.
- `backend/src/MealManager.Api/Controllers/DepositsController.cs`.
- Tests: `backend/tests/MealManager.IntegrationTests/DepositsFlowTests.cs`.

## Key decisions & why
- **Manager-created deposits auto-approve** (user decision) — the common flow is a manager taking cash and recording it; forcing a second self-approval click adds no audit value. The distinction stays visible via `ApprovedBy`/`ApprovedAt`.
- **Rejection leaves `ApprovedBy`/`ApprovedAt` null** — those columns mean "approved by/at"; a rejected deposit's reviewer is not recorded (state + ledger row suffice for Phase 2).
- **Routes `api/deposits` + approve/reject sub-actions** — plan §6 had no deposit routes at all; filled per API_CONVENTIONS same-PR rule and logged in the Decisions Log. Approve/reject mirror the members approve/reject verb style (`PUT …/{id}/approve`, 204).
- `status` query param parses the enum **outside** the EF expression tree (`Enum.Parse` is not SQL-translatable) — gotcha worth remembering for future enum filters.

## How to verify / test
- `cd backend && dotnet test --filter DepositsFlowTests` (5 facts: pending→approve→409s, manager auto-approve, member self-only + reject, filters/sort/pagination + status validation, tenant isolation incl. cross-mess approve → 404).
- `Phase2ValidatorTests.Deposit_Amount_Must_Be_Positive`.

## Follow-ups / known gaps
- P2-FE-4 deposits tab: ledger table + member "add deposit" + manager approve/reject toggles on Pending rows.
- Phase 3 summary math ("Total Money Given") must count **Approved deposits only** — Pending is not money on hand yet.
