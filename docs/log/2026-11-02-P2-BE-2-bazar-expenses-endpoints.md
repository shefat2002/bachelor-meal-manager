# P2-BE-2: BazarExpenses — POST/GET /api/expenses/bazar

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 backend session)
- **Phase / track**: Phase 2 / Backend

## What was built
Manager-only bazar recording: `POST /api/expenses/bazar` accepts a shopping run (`{date, buyerMemberId?, items:[{name, quantity, price}]}`) and writes **one row per item** (1–50), returning the created rows with computed `lineTotal = quantity × price` (201 + Location on the list route). Buyer defaults to the caller; an explicit other buyer must be an active member (else 404). `GET /api/expenses/bazar` filters by `buyerMemberId` + `from`/`to`, sorts by `date` or `price` (line total), paginates.

## Files touched
- `backend/src/MealManager.Application/Domain/BazarExpense.cs` — new entity.
- `backend/src/MealManager.Application/Features/Bazar/BazarContracts.cs` — DTO + request records.
- `backend/src/MealManager.Application/Features/Bazar/Commands/CreateBazarExpensesCommand.cs` — command + validator + handler.
- `backend/src/MealManager.Application/Features/Bazar/Queries/GetBazarExpensesQuery.cs` — list query + validator.
- `backend/src/MealManager.Application/Abstractions/TenantGuards.cs` — new shared `RequireManager(tenant, action)` guard (SuperAdmin already resolves to MealManager in TenantContext).
- `backend/src/MealManager.Api/Controllers/ExpensesController.cs` — `api/expenses` controller hosting bazar + utility sub-routes.
- Tests: `backend/tests/MealManager.IntegrationTests/BazarFlowTests.cs`.

## Key decisions & why
- **Quantity + unit price, line total computed (never stored)** — supports the P2-FE-3 drawer's name/qty/price rows with live auto-sum; keeps the stored value normalized if a UI ever edits quantities.
- **Batch POST = one row per item sharing date+buyer** — matches "one shopping trip" UX without a header/child BazarItems table (the plan deliberately has none).
- **Manager-only writes** — per RBAC skill matrix ("Member: read mess data, mark own meals, own deposits" — bazar is not listed); members read the table.
- `price` sort key sorts by the computed line total (SQL-translatable `Quantity * Price` on both Npgsql and sqlite).

## How to verify / test
- `cd backend && dotnet test --filter BazarFlowTests` (4 facts: multi-item create + buyer validation, member 403, filters/sort/pagination, tenant isolation).
- Validation ranges mirrored in `Phase2ValidatorTests` (quantity 0.01–99,999; price 0.01–999,999,999; name ≤ 200).

## Follow-ups / known gaps
- P2-FE-3 consumes this: filterable table + `+ Add Bazar` Sheet with `useFieldArray` and `Σ qty × price` total.
- No edit/delete of bazar rows yet — not in the Phase 2 task list; add later if managers need corrections (consider a P5 hardening item).
