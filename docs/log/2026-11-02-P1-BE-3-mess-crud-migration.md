# P1-BE-3: Mess entity, migration & CRUD

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Backend

## What was built

The tenant root + its API: `POST /api/messes` (creator auto-joins as **MealManager**, 201 + Location), paginated `GET /api/messes` (mine by membership; SuperAdmin sees all; `myRole` per mess), `GET /api/messes/{id}` (member-or-404), `PUT` (manager), `DELETE` (creator/SuperAdmin, cascades memberships + invites). Migration `Initial_Identity_Mess` (Identity tables + all Phase-1 domain tables) created and **applied to the Neon production branch**.

## Files touched

- `Application/Features/Messes/**` — CQRS per skill (commands/queries + validators + `MessDto`)
- `Infrastructure/ApplicationContext.cs` — model config (composite key MessId+MemberId, string-converted enums, unique invite token, indexes) — details in P1-BE-5 log
- `Infrastructure/Migrations/*Initial_Identity_Mess*`
- `Api/Controllers/MessesController.cs` — thin dispatch

## Key decisions & why

- **Npgsql ≠ libpq URIs**: Neon env URLs must be converted to keyword form — `ConvertToNpgsqlConnectionString` (drops `channel_binding`, defaults port 5432, forces SSL Mode=Require) runs at DbContext registration AND in the health check.
- **dotnet-ef `--connection` mangles `=`** — migrations apply via a temporary user-secrets swap to the sanitized unpooled URL (swap → update → restore; documented in backend/README).

## How to verify / test

`MessMemberFlowTests` (create/list/get/update/delete + cross-tenant 404) + live curl (mess created, creator role MealManager).

## Follow-ups / known gaps

- Mess settings beyond name (currency, lock day) arrive with later phases.
