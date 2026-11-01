# NEON-SETUP: Neon platform setup — Postgres, Managed Auth, Realtime wired into the Next.js app

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3) session, driven by user's explicit Neon setup instructions
- **Phase / track**: Phase 0 / Infrastructure + Frontend base

## What was built

The project is now live on Neon (org `org-green-water-52630304`, project `aged-dream-14739962` "bachelor-meal-manager", branch `production`, database `neondb`, Postgres 18.6, region ap-southeast-1). `neon.ts` declares `auth: true` and `preview.realtime.allowedOrigins: ["http://localhost:3000"]`; `neon deploy` provisions all three services and pulls 7 env vars into the root `.env.local`. A Next.js 16.4.3 app (App Router, TS strict, Tailwind, `src/` layout per `frontend/AGENTS.md`) was scaffolded in `frontend/` and wired to all three services: node-postgres pool (pooled connection), Managed Better Auth (`@neon/auth` with `proxy.ts` route protection), and Neon Realtime (sealed live queries + `useLiveQuery` + optimistic writes with `awaitTxId`). An infra-only `setup_probes` table powers a live demo/health dashboard.

## Files touched

- `neon.ts` — created by `neon init`, edited: realtime `allowedOrigins` (browser WebSocket origin allow-list)
- `.neon/`, root `.env.local` (gitignored) — Neon context + 7 service vars + 2 app vars (`NEON_AUTH_COOKIE_SECRET`, `NEXT_PUBLIC_NEON_REALTIME_URL`)
- root `package.json` — `@neon/config` dep (init) + `build`/`start`/`dev` scripts delegating to `frontend/`
- `frontend/` — full Next.js scaffold (create-next-app, moved in around existing `AGENTS.md`)
- `frontend/.env.local` — symlink → `../.env.local` (single source of truth; re-deploys update automatically)
- `frontend/db/migrations/0001_setup_probes.sql` — infra demo table (uuid PK, note, created_by, created_at)
- `frontend/src/lib/db.ts` — pg `Pool`, pooled URL, `sslmode=verify-full`, error guard
- `frontend/src/lib/auth/server.ts` — `createNeonAuth` (baseUrl + cookie secret)
- `frontend/src/lib/auth/client.ts` — `createAuthClient()` (same-origin proxy)
- `frontend/src/proxy.ts` — `auth.middleware`: loginUrl `/sign-in`, publicRoutes `/`, `/sign-in`, `/sign-up`
- `frontend/src/lib/realtime.ts` — `createRealtime` (secret, db `neondb`) + `probesQuery` rawSql live query
- `frontend/src/app/api/auth/[...path]/route.ts` — Better Auth catch-all proxy handler
- `frontend/src/app/api/probes/live/route.ts` — POST, session-gated, returns sealed query
- `frontend/src/app/api/probes/route.ts` — POST, session-gated, insert returning `pg_current_xact_id()`
- `frontend/src/app/api/health/db/route.ts` — GET, session-gated, `version()` + probe count
- `frontend/src/app/page.tsx`, `(auth)/sign-in`, `(auth)/sign-up`, `(dashboard)/dashboard/page.tsx`, `src/components/probes-panel.tsx`, `src/components/sign-out-button.tsx`, `src/app/layout.tsx`, `next.config.ts` (`turbopack.root`)

## Key decisions & why

- **Env via root `.env.local` + symlink into `frontend/`** — `neon env pull`/`deploy` always write the root file (where `.neon` lives); the symlink means redeployed values flow into the app with zero copy steps.
- **`pg` (node-postgres) + pooled URL, not `@neondatabase/serverless`** — per `neon-postgres` skill: serverless/HTTP driver is for edge runtimes; this app runs on Node (and will call Neon from the .NET backend via Npgsql for domain data anyway).
- **No Drizzle** — Realtime sealed queries use `rawSql` ("any other ORM, or none"); the canonical domain ORM is EF Core in the .NET backend. Avoids a duplicate ORM.
- **Neon Auth = Managed Better Auth for the *frontend*** — users/sessions live in `neon_auth` schema and branch with the DB. NOTE: the .NET backend's P1 identity tasks (ASP.NET Identity + JWT) are unaffected: the backend can verify Neon Auth JWTs via `NEON_AUTH_JWKS_URL` or keep its own plan — decision belongs to P1-BE agents/user.
- **`setup_probes` table is infra-only, droppable** — the domain schema (messes, meals, …) will be EF Core-owned; this table exists to prove/monitor the wiring.
- **Next 16 `cacheComponents` quirks** — `export const runtime`/`dynamic` are rejected at build; cookies-in-Suspense pattern used for the dashboard; runtime is Node by default.
- **Sign-out POST needs JSON content-type** (bare POST gets 415) — the UI client sends it correctly; curl testers must pass `-H 'content-type: application/json' -d '{}'`.
- **Cookie-less API requests get a 307 redirect from `proxy.ts`** (skill-default protection) rather than JSON 401; every API route ALSO has an in-route session check as defense in depth. If pure-JSON 401s are wanted for machine clients, add `"/api"` to `publicRoutes` and rely on in-route checks.

## How to verify / test

```bash
npm run build                 # root script → frontend build (passes)
npm start                     # then http://localhost:3000
```
- Sign up at `/sign-up` → lands on `/dashboard` (live query status: **live**)
- `/api/health/db` (signed in): returns Postgres 18.6 version + probe count
- External insert while dashboard open: `psql "$(grep ^DATABASE_URL_UNPOOLED= .env.local | cut -d= -f2- | tr -d '\"')" -c "INSERT INTO setup_probes (note, created_by) VALUES ('x','psql');"` → row appears in browser without reload
- Signed-out `/dashboard` → 307 to `/sign-in`; wrong password → `INVALID_EMAIL_OR_PASSWORD`

## Follow-ups / known gaps

- **P0-FE-1 not fully done**: Shadcn UI init + Prettier still missing (scaffold itself exists). Complete under P0-FE-1.
- `neon.ts` `allowedOrigins` must gain the production origin before first real deployment.
- Backend (P0-BE-2) connection strings should point at Neon (pooled for app, `DATABASE_URL_UNPOOLED`/direct for EF migrations), not dockerized Postgres, for the canonical DB.
- `setup_probes` + dashboard demo can be dropped once real features land.
- `npm audit` reports 16 moderate vulns in the scaffold dep tree (not investigated this task).
- The e2e user is `e2e-neon@bmm.local` / `neon-E2E-passw0rd` (test account, production branch — delete or keep consciously).
