# P2-BE-3: UtilityTypes — manager-scoped CRUD per mess

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 backend session)
- **Phase / track**: Phase 2 / Backend

## What was built
Per-mess utility category management: `POST /api/utility-types` (201), `GET /api/utility-types` (name-ordered, paginated), `PUT /api/utility-types/{id}` (rename/description/`isActive`), `DELETE /api/utility-types/{id}` (204; **409 when any payment references the type** — deactivate instead). All writes manager-only; reads for all mess members. Names are unique per mess (case-insensitive check → 409; DB unique index `(MessId, Name)` as the backstop).

## Files touched
- `backend/src/MealManager.Application/Domain/UtilityType.cs` — new entity (`Name` ≤100, `Description?` ≤300, `IsActive`).
- `backend/src/MealManager.Application/Features/Utilities/UtilityTypeContracts.cs` — DTO + request records.
- `backend/src/MealManager.Application/Features/Utilities/Commands/UtilityTypeCommands.cs` — create command + validator + shared `EnsureNameFreeAsync` guard.
- `backend/src/MealManager.Application/Features/Utilities/Commands/UpdateUtilityTypeCommand.cs`, `Commands/DeleteUtilityTypeCommand.cs`.
- `backend/src/MealManager.Application/Features/Utilities/Queries/GetUtilityTypesQuery.cs`.
- `backend/src/MealManager.Api/Controllers/UtilityTypesController.cs`.
- Tests: `backend/tests/MealManager.IntegrationTests/UtilitiesFlowTests.cs` (shared with P2-BE-4).

## Key decisions & why
- **Route `api/utility-types` (kebab)** — the plan only defined payment routes; this route was a gap filled per `API_CONVENTIONS.md` "update in the same PR" rule (Decisions Log entry added).
- **Soft-disable via `isActive`, hard DELETE guarded by 409** — payment history must keep referencing types; the P2-FE-4 utilities tab shows inactive types disabled and uses deactivate for anything with history.
- Case-insensitive duplicate check in the handler (DB unique index is exact-case) — prevents "WiFi" vs "wifi" doubles.
- Utility types list is **not** `IListQuery` (no date filters/sort) — plain paginated, name-ordered; documented in API_CONVENTIONS §3.

## How to verify / test
- `cd backend && dotnet test --filter UtilitiesFlowTests` (covers CRUD, duplicate 409, member 403, delete-with-payments 409, tenant isolation incl. same-name-different-mess allowed).
- Validator rules in `Phase2ValidatorTests` (`UtilityType_Name_Rules`).

## Follow-ups / known gaps
- P2-FE-4 utilities tab: type cards + paid/pending status for the active month (derive from `GET /api/expenses/utility?from=&to=`).
