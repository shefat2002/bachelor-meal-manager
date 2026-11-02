# P2-BE-4: UtilityPayments — POST/GET /api/expenses/utility

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 backend session)
- **Phase / track**: Phase 2 / Backend

## What was built
Manager-only utility payment recording: `POST /api/expenses/utility` (`{utilityTypeId, amount, paymentDate, note?}` → 201 with the payment DTO including `utilityTypeName`), where the type must belong to the active mess (cross-mess type ids → 404, no leakage). `GET /api/expenses/utility` filters by `utilityTypeId` + `from`/`to`, sorts by payment date, paginates, and joins the type name server-side.

## Files touched
- `backend/src/MealManager.Application/Domain/UtilityPayment.cs` — new entity (`Amount` 18,2; `PaymentDate`; `Note?`; `CreatedBy`).
- `backend/src/MealManager.Application/Features/Utilities/UtilityPaymentContracts.cs` — DTO + request record.
- `backend/src/MealManager.Application/Features/Utilities/Commands/CreateUtilityPaymentCommand.cs`.
- `backend/src/MealManager.Application/Features/Utilities/Queries/GetUtilityPaymentsQuery.cs`.
- `backend/src/MealManager.Api/Controllers/ExpensesController.cs` — utility sub-routes alongside bazar.
- Tests: `backend/tests/MealManager.IntegrationTests/UtilitiesFlowTests.cs`.

## Key decisions & why
- **No `paid/pending` status column** — the design spec's per-month paid/pending card status is *derived*: a type is paid for a month iff a payment exists in that window. `GET /api/expenses/utility?from=&to=` gives the frontend everything it needs; a stored status would drift from reality.
- **Payment → type scoped by mess at write time** — `UtilityTypeId` is trusted only after resolving within the tenant; this is the NOT_FOUND-masks-cross-tenant pattern from P1.
- Sort whitelist for this endpoint is just `date` (amount sorting has no UI need on payments; deposits keep `amount`).

## How to verify / test
- `cd backend && dotnet test --filter UtilitiesFlowTests` — create+list with type filter/date range/order, member 403, unknown type 404, tenant isolation.
- `Phase2ValidatorTests.UtilityPayment_Amount_Must_Be_Positive`.

## Follow-ups / known gaps
- P2-FE-4 utilities tab "record payment" Sheet posts here; month window comes from the top-bar month switcher.
