# RESTRUCTURE: Database moved to backend-only; Neon = Postgres provider; Aspire for local dev; duplicate installs removed

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3), driven by user's correction instructions
- **Phase / track**: Phase 0 / Repository structure + Infrastructure decisions

## What was built

The repository was restructured to match the user's architecture: **the database is connected from the backend only** (EF Core/Npgsql in the .NET solution), Neon is used **solely as the hosted Postgres provider**, and **local development will be orchestrated with ASP.NET Aspire**. All Neon Auth / Neon Realtime / direct `pg` wiring that the previous session (`2026-11-02-NEON-SETUP-neon-services-wiring.md`) had added to the Next.js app was removed, the duplicate npm installs were collapsed to a single one in `frontend/`, and every affected doc/skill now records the new canonical rules. **That Neon-SETUP log is now superseded — read this one for current truth.**

## Files touched

**Deleted (frontend de-wiring + duplicate installs)**
- root `package.json`, `package-lock.json`, `node_modules/` — duplicate install; `frontend/` is the single npm root now
- `frontend/frontend/` — stray nested directory containing only a `.next` build artifact
- `frontend/.next/` — stale build (regenerated clean after cleanup)
- `frontend/.env.local` — symlink to root `.env.local`; the frontend must not see DB credentials
- `frontend/db/` (setup_probes migration), `frontend/src/lib/db.ts`, `frontend/src/lib/realtime.ts`, `frontend/src/lib/auth/`, `frontend/src/proxy.ts`, `frontend/src/app/api/` (auth catch-all, probes, health/db), `frontend/src/app/(auth)/`, `frontend/src/app/(dashboard)/`, `frontend/src/components/probes-panel.tsx`, `frontend/src/components/sign-out-button.tsx`
- `.agents/skills/neon-auth/`, `.agents/skills/neon-realtime/` — no longer used (Neon = Postgres only)

**Edited (frontend)**
- `frontend/package.json` — removed `@neon/auth`, `@neon/realtime`, `@neon/realtime-react`, `pg`, `@types/pg`; lockfile pruned via `npm install`
- `frontend/src/app/page.tsx` — neutral placeholder landing page (old one linked to removed Neon-auth pages)
- `frontend/src/app/layout.tsx` — metadata no longer says "on Neon Postgres"
- `frontend/next.config.ts` — comment updated (no parent lockfile anymore)
- `frontend/AGENTS.md` — **restored from git** (the create-next-app scaffold had overwritten it), plus new **§0 "Hard Boundary — No Direct Database Access"**, plus the Next.js agent-rules block re-appended

**Edited (config)**
- `neon.ts` — trimmed to Postgres-only: removed `auth: true` and `preview.realtime.allowedOrigins`; kept branch TTL policy; comment states Neon's role
- `.gitignore` (root) — added `.next/`

**Edited (docs & skills — Neon Postgres + Aspire + backend-only DB)**
- `AGENTS.md` (root) — §1 stack bullets, §2 repo map rows for `neon.ts`/`.env.local`, §4 four new canonical decisions, §5 commands (Aspire run line; no root package.json; no local Postgres)
- `backend/AGENTS.md` — stack table (Neon Postgres w/ pooled vs unpooled strings, Aspire line) + backend-only-DB guideline
- `docs/BACKEND_PROJECT_PLAN.md`, `docs/BACKEND_PROJECT_OVERVIEW.md` — §2.1/Technology Stack updated
- `docs/PROJECT_PHASES.md` — architecture paragraph + Phase 0 (docker-compose → Aspire AppHost + Neon connection strings) + exit criteria
- `docs/TASK_CHECKLIST.md` — P0-BE-2 rewritten (Aspire AppHost + Neon connection strings; now *depends: P0-BE-1*), Status Snapshot updated, 4 new Decisions Log rows (previous Neon-wiring row marked SUPERSEDED, not deleted)
- `.agents/skills/bmm-efcore-multitenancy` — new "Database hosting — Neon (backend-only)" section (pooled vs unpooled, TestContainers for tests)
- `.agents/skills/bmm-rabbitmq-workers`, `bmm-observability`, `bmm-cicd-github-actions` — docker-compose local-dev references updated to Aspire (observability stack may still use compose)

## Key decisions & why

- **DB backend-only** — user decision; also the correct shape for the planned architecture (EF Core global query filters, Orleans grains, CQRS). Frontend gets data exclusively via the ASP.NET API (JWT + `X-Mess-Id`).
- **Neon = Postgres provider only** — identity goes back to the original plan: ASP.NET Core Identity + JWT (P1-BE-1/2). Async UI updates will come from the backend (SSE/WebSocket/polling, P4-BE-4), not Neon Realtime. The provisioned-but-unused Neon Auth/Realtime services can be decommissioned in the Neon console (user action; nothing deployed in this task — `neon.ts` was only edited, `neon deploy` was **not** run).
- **Aspire for local dev** — user decision; AppHost orchestrates Api, Orleans host, the Next.js dev server (`AddNpmApp`-style resource), and Redis/RabbitMQ containers. No docker-compose for app infra; Postgres is never run locally (Neon connection strings from root `.env.local`). Only Phase 4's Prometheus/Grafana stack may keep compose.
- **Connection-string split** — `DATABASE_URL` (pooled, `-pooler` host) for the app runtime; `DATABASE_URL_UNPOOLED` for `dotnet ef` migrations (DDL over PgBouncer transaction pooling fails).
- **Root `.env.local` kept as-is** (gitignored) — it holds the Neon connection strings the *backend* will consume. The now-dead `NEON_AUTH_*` / `NEON_REALTIME_*` vars were left in place rather than edited; harmless, can be pruned anytime.
- **`frontend/AGENTS.md` restored from git rather than rewritten** — the scaffold had clobbered the project's frontend architecture doc with a generic Next.js notice; git held the original.

## How to verify / test

```bash
cd frontend && npm install && npm run lint && npm run build   # all pass (verified this task)
```
- `ls` repo root: no `package.json`/`node_modules`; `frontend/frontend/` gone
- `grep -ri 'neon\|"pg"' frontend/src frontend/package.json` → no hits
- `neon.ts` contains neither `auth:` nor `realtime`

## Follow-ups / known gaps

- **P0-BE-1** should scaffold the solution *including* `MealManager.AppHost` (Aspire) + `ServiceDefaults`; P0-BE-2 (rewritten) wires Redis/RabbitMQ containers + Neon connection strings into it.
- The `setup_probes` table still exists in the Neon `neondb` database (droppable; no code references it anymore). The e2e test user `e2e-neon@bmm.local` from the superseded session can be deleted.
- Neon Auth/Realtime services are still provisioned on the Neon project — decommission via Neon console if desired (they are now undeclared in `neon.ts`).
- Root `.env.local` still contains unused `NEON_AUTH_*`/`NEON_REALTIME_*`/`NEXT_PUBLIC_NEON_REALTIME_URL` vars — safe to delete; kept to avoid touching a secrets file.
- P0-FE-1 remainder unchanged: Shadcn UI init + Prettier.
