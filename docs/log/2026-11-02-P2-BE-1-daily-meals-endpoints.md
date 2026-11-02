# P2-BE-1: DailyMeals — POST/GET /api/meals

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 backend session)
- **Phase / track**: Phase 2 / Backend

## What was built
Bulk upsert + list endpoints for daily meal counts. `POST /api/meals` takes one date plus 1–100 entries (`{memberId, breakfast, lunch, dinner}`), creates or **overwrites** the row per (mess, member, date) via the unique index, and returns the resulting `DailyMealDto[]` (includes computed `totalUnits = B+L+D`). `GET /api/meals` filters by `memberId` + inclusive `from`/`to`, sorts by `date` (whitelist of one), paginates per the standard envelope. Plain members can only log their own `memberId`; Meal Managers (and SuperAdmin via the tenant pipeline) can log for any active member — targets that are not active members of the mess resolve to 404 (no cross-mess leakage).

## Files touched
- `backend/src/MealManager.Application/Domain/DailyMeal.cs` — new entity (Breakfast/Lunch/Dinner `decimal(4,1)`).
- `backend/src/MealManager.Application/Features/Meals/MealsContracts.cs` — DTO + request records.
- `backend/src/MealManager.Application/Features/Meals/Commands/SubmitDailyMealsCommand.cs` — command + validator + upsert handler.
- `backend/src/MealManager.Application/Features/Meals/Queries/GetMealsQuery.cs` — list query + validator (shared `ListQueryRules`).
- `backend/src/MealManager.Application/Common/ListQuery.cs` — new shared list-query validation (page/pageSize/from≤to/sortBy whitelist/sortDir) used by all Phase 2 list endpoints.
- `backend/src/MealManager.Api/Controllers/MealsController.cs` — thin controller.
- Tests: `backend/tests/MealManager.IntegrationTests/MealsFlowTests.cs`, `DomainTestSetup.cs` (shared mess+member fixture), `backend/tests/MealManager.UnitTests/Phase2ValidatorTests.cs`.

## Key decisions & why
- **Guest meals fold into B/L/D counts** (user decision) — no separate guest column; `API_CONVENTIONS.md` §5 "guests included" honored literally. The P2-FE-1 "guest meal" quick control will just bump the relevant slot count.
- **POST returns 200 (upsert), not 201/Location** — batch overwrite semantics make a single Location meaningless.
- **Count rule: 0–10 inclusive, 0.5 steps** — mirrors skill `bmm-cqrs-mediatr` validator example; the half-step check is `Truncate(v*2) == v*2`. Frontend Zod must mirror 1:1.
- **pageSize > 100 → 400 VALIDATION_ERROR** (not clamped) — `API_CONVENTIONS.md` §3 rule, now actually enforced by validators (Phase 1 endpoints predate it).

## How to verify / test
- `cd backend && dotnet test --filter MealsFlowTests` (5 facts: upsert/filters, member-own-only, count validation, query-param validation, tenant isolation).
- Manual: `POST /api/meals` with `X-Mess-Id` header; re-submit same member+date → same row id, new counts.

## Follow-ups / known gaps
- P2-FE-1 daily sheet consumes `GET /api/meals?from=<d>&to=<d>&pageSize=100`; P2-FE-2 monthly matrix loop-fetches pages (`from`/`to` = month bounds).
- No month-lock rejection yet — reserved for P3-BE-5 (409 CONFLICT).
