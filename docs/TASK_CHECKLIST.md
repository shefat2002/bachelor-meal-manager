# Task Checklist — Single Source of Truth for Progress

> **Any agent starting a session in this repo: read this file first.**
> It answers "where are we?" without re-explaining the project. Full workflow protocol: root [AGENTS.md](../AGENTS.md). Phase goals and rationale: [PROJECT_PHASES.md](./PROJECT_PHASES.md).

**Rules for updating this file**
1. Mark finished tasks `- [x]`; add `⏳ in-progress (by <agent>, <date>)` when you start one; remove the marker when done.
2. After every completed task, update the **Status Snapshot** below.
3. Append durable decisions to the **Decisions Log** (bottom). Never delete entries.
4. Write the matching log file in [docs/log/](./log/README.md) — future agents read those instead of re-deriving context.

---

## Status Snapshot  *(update after every task)*

| Field | Value |
|---|---|
| **Current phase** | Phase 2 — Core Domain: Meals, Bazar & Utilities |
| **Last completed** | **Phase 1 complete** — all 10 tasks (P1-BE-1…5, P1-FE-1…5); migration applied to Neon; tests green (10 unit + 12 sqlite e2e + PG isolation suite in CI); logs in `docs/log/2026-11-02-P1-*.md` |
| **In progress** | — |
| **Next up (parallelizable)** | P2-BE-1/2 (meals + bazar endpoints) lead; P2-FE-* follow on contracts |
| **Blocked** | — |
| **Last updated** | 2026-11-02 by ZCode (Phase 1) |

---

## Phase 0 — Foundation & Scaffolding

- [x] **P0-BE-1** Scaffold .NET 10 solution: `Api`, `OrleansHost`, `Application` (CQRS), `Infrastructure` (EF Core), test projects; `/health` endpoint — *no deps*
- [x] **P0-BE-2** ASP.NET **Aspire AppHost** (Redis + RabbitMQ container resources, Next.js dev-server resource) + **Neon Postgres** connection-string configuration (`DATABASE_URL` pooled for app, `DATABASE_URL_UNPOOLED` for EF migrations; no local Postgres container) — *depends: P0-BE-1*
- [x] **P0-FE-1** Scaffold Next.js App Router (TS strict) + Tailwind + Shadcn UI + ESLint/Prettier per `frontend/AGENTS.md` — *no deps*
- [x] **P0-FE-2** Core libs: `lib/api-client.ts` (Axios), `lib/query-client.ts`, `lib/auth.ts` skeleton, env config — *depends: P0-FE-1*
- [x] **P0-IN-1** GitHub Actions CI skeleton: backend build+test, frontend lint+build, on push to `main` — *depends: P0-BE-1, P0-FE-1*
- [x] **P0-DX-1** Write `docs/API_CONVENTIONS.md`: response envelope, `PaginatedResult<T>`, error codes, filtering/sorting/pagination query params — *no deps*
- [x] **P0-DX-2** Normalize legacy "Hostel/HostelId" wording in `docs/BACKEND_PROJECT_OVERVIEW.md` + backend section of `docs/SKILL_BUILDER.md` to Mess/MessId — *low priority, anytime*

## Phase 1 — Identity, Auth & Multi-Tenancy Core

- [x] **P1-BE-1** ASP.NET Core Identity: phone-number accounts, password + OTP login — *depends: P0-BE-1/2*
- [x] **P1-BE-2** JWT endpoints: login, refresh, logout; role claims (SuperAdmin, MealManager, Member) — *depends: P1-BE-1*
- [x] **P1-BE-3** Mess entity + migrations + `POST/GET/PUT/DELETE /api/messes` (creator → Meal Manager) — *depends: P1-BE-2*
- [x] **P1-BE-4** MembersMesses M2M + invite-link join + approval workflow + `PUT /api/members/{id}/role` — *depends: P1-BE-3*
- [x] **P1-BE-5** EF Core global query filters by `MessId` + tenant-isolation integration tests — *depends: P1-BE-3*
- [x] **P1-FE-1** `(auth)` route group: login/verify screens with RHF+Zod forms — *depends: P1-BE-2 (contract), P0-FE-1*
- [x] **P1-FE-2** Token storage/refresh + Axios interceptors (Bearer + `X-Mess-Id` auto-attach) — *depends: P1-FE-1*
- [x] **P1-FE-3** Dashboard app shell: sidebar, top utility bar, mess switcher, mobile bottom nav — *depends: P1-FE-2*
- [x] **P1-FE-4** RBAC: `<Authorize roles={[...]}>` wrapper + middleware route protection — *depends: P1-FE-2*
- [x] **P1-FE-5** Mess create/join UI + invite link handling + pending-approvals actions — *depends: P1-BE-4, P1-FE-3*

## Phase 2 — Core Domain: Meals, Bazar & Utilities

- [ ] **P2-BE-1** DailyMeals: `POST/GET /api/meals` (per member/date B/L/D counts, filtering+sorting+pagination) — *depends: P1-BE-5*
- [ ] **P2-BE-2** BazarExpenses: `POST/GET /api/expenses/bazar` (items, price, date, buyer) — *depends: P1-BE-5*
- [ ] **P2-BE-3** UtilityTypes: manager-scoped CRUD per mess (Electricity, Wi-Fi, Cook Salary…) — *depends: P1-BE-5*
- [ ] **P2-BE-4** UtilityPayments: `POST/GET /api/expenses/utility` linked to UtilityTypes — *depends: P2-BE-3*
- [ ] **P2-BE-5** Member deposits ledger + manager approval endpoints — *depends: P1-BE-5*
- [ ] **P2-FE-1** Meal sheet — daily entry view (member rows, `- 0 0.5 1 2 +` steppers, guest meals, optimistic updates) — *depends: P2-BE-1, P1-FE-3*
- [ ] **P2-FE-2** Meal sheet — monthly matrix grid (sticky name col, day columns, monthly totals) — *depends: P2-FE-1*
- [ ] **P2-FE-3** Expenses hub — bazar tab: filterable table + `+ Add Bazar` drawer with dynamic rows & auto-sum — *depends: P2-BE-2*
- [ ] **P2-FE-4** Expenses hub — utilities tab (type cards, paid/pending) + deposits ledger tab with approval toggles — *depends: P2-BE-4, P2-BE-5*
- [ ] **P2-FE-5** Members directory: role badges, invite modal (link/QR + expiry), pending approvals banner — *depends: P1-FE-5*

## Phase 3 — Financial Logic: Orleans, Summary & Settlement

- [ ] **P3-BE-1** Orleans silo wiring in Api/host: clustering config, grain storage (Redis/PostgreSQL) — *depends: Phase 2 BE tasks*
- [ ] **P3-BE-2** `MessGrain` + `MemberGrain`: running totals (meals, expenses, deposits), sequential concurrency, persistence — *depends: P3-BE-1*
- [ ] **P3-BE-3** `GET /api/summary/messes/{messId}`: meal rate, individual costs, surplus/arrear, on-hand balance (grain-computed) — *depends: P3-BE-2*
- [ ] **P3-BE-4** Redis caching for summary/meal rate + invalidation on meals/expenses/deposits writes — *depends: P3-BE-3*
- [ ] **P3-BE-5** Month lock: freeze edits after settlement — *depends: P3-BE-3*
- [ ] **P3-FE-1** Overview dashboard: 4 KPI cards (meal rate, on-hand, expenditure, my status) + today's meal snapshot + recent activity — *depends: P3-BE-3*
- [ ] **P3-FE-2** Recharts: daily bazar spend vs meal volume trend chart — *depends: P3-FE-1*
- [ ] **P3-FE-3** Master settlement table + export/close bar (PDF button, Lock Month toggle) — *depends: P3-BE-5*
- [ ] **P3-FE-4** Summary auto-invalidation wired to meal/expense/deposit mutations — *depends: P3-BE-3*

## Phase 4 — Async Jobs, Notifications & Observability

- [ ] **P4-BE-1** RabbitMQ infrastructure: topology, publisher/consumer abstractions, retry + DLQ — *depends: Phase 2*
- [ ] **P4-BE-2** Invitation/notification worker (email alerts on invites, approvals, role changes) — *depends: P4-BE-1*
- [ ] **P4-BE-3** Monthly settlement report generation (PDF) as background job — *depends: P4-BE-1, P3-BE-3*
- [ ] **P4-BE-4** Job-status endpoint + SSE/WebSocket (or polling) notification stream — *depends: P4-BE-3*
- [ ] **P4-BE-5** Serilog structured logging across services + request logging — *anytime after Phase 0*
- [ ] **P4-BE-6** Prometheus `/metrics` + health checks + Grafana dashboards + compose extension — *depends: P4-BE-5*
- [ ] **P4-FE-1** Notification popover + async-report-ready alerts — *depends: P4-BE-4*
- [ ] **P4-FE-2** Report download flow from notification — *depends: P4-FE-1*

## Phase 5 — Hardening, CI/CD & Release

- [ ] **P5-IN-1** Full CI/CD: dotnet test → Docker build/push to registry; frontend build+deploy — *depends: P4*
- [ ] **P5-BE-1** Security pass: authz audit on every endpoint, rate limiting, validator fuzzing, token edge cases — *depends: Phase 4*
- [ ] **P5-BE-2** Test hardening: unit+integration coverage across domains; tenant-isolation suite green — *depends: Phase 4*
- [ ] **P5-FE-1** Critical frontend flow tests (login, meal entry, expense add, settlement view) — *depends: Phase 4*
- [ ] **P5-DX-1** Seed/demo data script + deployment README — *depends: P5-IN-1*

---

## Decisions Log  *(append-only)*

| Date | Decision | Context |
|---|---|---|
| 2026-11-02 | Tenant key is `MessId` everywhere; `HostelId` in older docs is legacy wording | Conflict between `docs/BACKEND_PROJECT_OVERVIEW.md` and `docs/BACKEND_PROJECT_PLAN.md`/`backend/AGENTS.md`; majority + agent instructions use Mess |
| 2026-11-02 | Backend targets .NET 10 / C# 14 | `docs/BACKEND_PROJECT_PLAN.md` §2.1 |
| 2026-11-02 | Project skills live in `.agents/skills/` (12 skills, per `docs/SKILL_BUILDER.md`) | Skills encode build conventions so agents don't re-derive them |
| 2026-11-02 | Progress protocol: tick task → update Status Snapshot → append Decisions Log → write `docs/log/` entry | Root `AGENTS.md` §3 |
| 2026-11-02 | Production Postgres is hosted on **Neon** (`neon.tech`), not self-hosted/dockerized Postgres; Neon agent skills (`neon`, `neon-postgres`, `neon-postgres-branches`) installed in `.agents/skills/` from `neondatabase/agent-skills` | User decision 2026-11-02; EF Core `Npgsql` connection strings point at Neon (use pooled connection string). Local docker-compose Postgres may still be used for offline dev/tests, but Neon is the canonical DB. Neon Auth/Functions/Storage skills intentionally NOT installed — project has its own JWT auth, Orleans, RabbitMQ |
| 2026-11-02 | Neon project `aged-dream-14739962`, branch `production`, db `neondb`, all three services (Postgres 18.6, Managed Better Auth, Realtime) deployed via `neon.ts` and wired into the Next.js frontend; env flows root `.env.local` → symlinked `frontend/.env.local` | User-provided org/project ids; details + verification commands in `docs/log/2026-11-02-NEON-SETUP-neon-services-wiring.md`. Frontend auth = Managed Better Auth (`@neon/auth`); .NET P1 identity tasks unaffected and may verify Neon JWTs via `NEON_AUTH_JWKS_URL` or keep ASP.NET Identity plan — decide in P1. **SUPERSEDED 2026-11-02 (restructure): frontend Neon wiring removed — see following rows** |
| 2026-11-02 | **Database connectivity is backend-only.** All Neon Auth/Realtime/pg wiring removed from `frontend/` (db.ts, proxy.ts, api routes, probes demo, auth pages, `frontend/db/`, `.env.local` symlink). Frontend consumes the ASP.NET Core API exclusively | User decision 2026-11-02; canonical rule now in root `AGENTS.md` §4 + `frontend/AGENTS.md` §0. Supersedes the Neon-services-wiring entry above (its log file stays for history) |
| 2026-11-02 | **Neon is used only as hosted Postgres.** `neon.ts` trimmed (no `auth`, no realtime preview config); `neon-auth`/`neon-realtime` skills removed from `.agents/skills/` | Identity = ASP.NET Core Identity + JWT (P1-BE-*); async UI updates arrive via the backend API. Neon Auth/Realtime services may be decommissioned in the Neon console |
| 2026-11-02 | **Local dev orchestration = ASP.NET Aspire** AppHost (API, Orleans host, Next.js dev server, Redis/RabbitMQ containers); no docker-compose for app infra; Postgres stays on Neon (pooled `DATABASE_URL` / `DATABASE_URL_UNPOOLED` for EF migrations) | User decision 2026-11-02; P0-BE-2 rewritten from docker-compose to Aspire (now depends on P0-BE-1). Phase 4 observability stack (Prometheus/Grafana) may still use compose |
| 2026-11-02 | **`frontend/` is the single npm root** — root `package.json`/`package-lock.json`/`node_modules/` removed, stray `frontend/frontend/` build dir deleted | Duplicate node_modules installs (root + frontend); `neon.ts` stays at repo root for the `neon` CLI, which needs no installed deps |
| 2026-11-02 | Solution layout: `backend/MealManager.sln`, `src/{MealManager.Api, .Application (MediatR 14.2 + FluentValidation 12.2), .Infrastructure (EF Core 10 + Npgsql), .OrleansHost, .AppHost, .ServiceDefaults}`, `tests/{MealManager.UnitTests, .IntegrationTests}` (xUnit); namespaces `MealManager.*`; SDK pinned `backend/global.json` (10.0.403, rollForward latestFeature) | P0-BE-1; per checklist task + restructure log guidance |
| 2026-11-02 | Orleans silo will be wired **in the API process** in Phase 3 (skill `bmm-orleans-grains`); `MealManager.OrleansHost` exists now as a worker placeholder for future dedicated silo hosts | P0-BE-1 |
| 2026-11-02 | Health endpoints: explicit `/health` (liveness) + `/health/ready` (readiness) per skill `bmm-observability` — Aspire `MapDefaultEndpoints` (`/health`+`/alive`, dev-only) deliberately NOT used; readiness gains dependency checks in later phases | P0-BE-1 |
| 2026-11-02 | Aspire 13.6.4 AppHost: `api` fixed at `http://localhost:5080`, `frontend` npm dev on :3000 via `PORT`; **Redis/RabbitMQ containers are skipped with a warning when Docker is unavailable** (dev machine has no Docker; GitHub CI runners do); Neon strings live in AppHost user-secrets: `messdb` (pooled `DATABASE_URL`) + `messdb-migrations` (unpooled) → injected into api as `ConnectionStrings__*` | P0-BE-2; setup commands in `backend/README.md` |
| 2026-11-02 | Shadcn UI via new CLI: style `radix-nova` (Radix primitives + lucide, Geist fonts). Design tokens in `globals.css`: primary **indigo-600**, dark bg **slate-950**, semantics as CSS vars `--surplus/--arrear/--pending` (+muted) with `badge-surplus/badge-arrear/badge-pending` classes and `.num` = `font-mono tabular-nums`. Prettier (printWidth 100, tailwind plugin) is the formatter (`npm run format`) | P0-FE-1; tokens fixed per `docs/FRONTEND_DESIGN_OVERVIEW.md` §1 |
| 2026-11-02 | Frontend core libs: Axios instance w/ Bearer + `X-Mess-Id` request interceptor (mess-id source installed by MessProvider in P1) and single-flight 401 refresh/replay skeleton; TanStack Query defaults (staleTime 30s, retry≤2 skip 4xx); access token in memory only | P0-FE-2; skills `bmm-app-router-multitenancy`, `bmm-server-state` |
| 2026-11-02 | CI = one `.github/workflows/ci.yml`, two parallel jobs (backend setup-dotnet via `global.json` → restore/build/test; frontend setup-node via `frontend/.nvmrc` → npm ci/lint/build); no Docker/registry/deploy until Phase 5 | P0-IN-1; skill `bmm-cicd-github-actions` Phase-0 shape |
| 2026-11-02 | API contract fixed in `docs/API_CONVENTIONS.md`: `ApiErrorResponse {type,title,status,detail,errors,traceId}`; error codes VALIDATION_ERROR/TENANT_REQUIRED/UNAUTHENTICATED/FORBIDDEN/NOT_FOUND/CONFLICT/RATE_LIMITED/INTERNAL_ERROR; `PaginatedResult<T> {items,page,pageSize,totalCount}`; `?page=&pageSize≤100&sortBy=<whitelist>&sortDir=asc|desc&from=&to=`; camelCase; dates `YYYY-MM-DD`; no versioning yet | P0-DX-1; binding for backend + frontend `types/api.ts` |
| 2026-11-02 | **Refresh-token contract**: rotating refresh tokens travel ONLY in the HttpOnly `bmm_refresh` cookie (Path=/api/auth, SameSite=Lax, Secure outside dev, 7d); JSON responses carry `accessToken`+`user` only. Reuse of a rotated token revokes ALL the user's sessions. Frontend `apiClient` uses `withCredentials` + backend CORS AllowCredentials for localhost:3000 | P1-BE-2; skill bmm-app-router-multitenancy; middleware route protection reads the same cookie |
| 2026-11-02 | **JWT validation reads config inside the lazy JwtBearer options lambda** (not at registration) — user-secrets load in the test host too, so eager reads minted/validated with different keys | P1-BE-2 test-host lesson; 15-min access tokens, HMAC key in Api user-secrets, claims sub/phone/name/globalRole + Role=SuperAdmin |
| 2026-11-02 | **Npgsql rejects libpq URI strings** — Infrastructure `ConvertToNpgsqlConnectionString` converts Neon URIs (postgresql://user:pass@host/db → keyword form; Uri.Port −1 → 5432; drops libpq-only params like channel_binding; forces SSL Mode=Require). `dotnet ef database update --connection` mangles `=` — pass the connection via user-secrets instead (README documents the swap-apply-restore flow) | P1-BE-3; migration `Initial_Identity_Mess` applied to Neon production branch via unpooled URL |
| 2026-11-02 | **Tenant resolution is a MediatR pipeline behavior** (Validation → Tenant order): `ITenantRequest` marker + X-Mess-Id header → membership check → scoped `TenantContext`; missing header 400 TENANT_REQUIRED, non-member 403 FORBIDDEN, SuperAdmin bypasses. `HasQueryFilter(MessId)` on MemberMess + InviteLinks; cross-mess queries use `IgnoreQueryFilters()` consciously | P1-BE-4/5; skills bmm-cqrs-mediatr + bmm-efcore-multitenancy |
| 2026-11-02 | Members workflow: single-use invite tokens (7d expiry, url-safe), join → **Pending** membership → manager approve/reject; role changes + leave/remove guarded by "at least one active Meal Manager" (409). Handlers inject `IApplicationContext` directly for Phase 1 (repository layer deferred) | P1-BE-4; pragmatic layering note |
| 2026-11-02 | **Test strategy**: sqlite-in-memory `WebApplicationFactory` e2e suite (runs everywhere; EF10 needs `IDbContextOptionsConfiguration<T>` removal, and each cookie-carrying stage needs a FRESH HttpClient — the test client caches cookies per instance) + TestContainers Postgres isolation suite as `[SkippableFact]` (runs in CI where Docker exists, skips locally). OTP dev code returned in JSON only in Development (SMS arrives Phase 4) | P1-BE-5; live e2e against Neon also verified via curl |
| 2026-11-02 | **App shell lives at `(dashboard)/dashboard/[messId]/layout.tsx`** (deviation from the skill's `(dashboard)/layout.tsx` — the shell's nav/switcher need the active mess, which only exists under `[messId]`). Next 16 cacheComponents: `params`/`useParams` reads must sit inside `<Suspense>`; `/dashboard` is the mess selector page | P1-FE-3; MessProvider validates GUID, renders no-access screen, installs the api-client mess-id source, drops old mess caches on switch |
| 2026-11-02 | Health split: `/health` = live-tag checks only (liveness); `/health/ready` = all incl. real Npgsql ping to Neon (converted string). Middleware (Edge) protects `/dashboard/*` + `/invite/*` via the `bmm_refresh` cookie → `/login?next=` | P1-BE-2 + P1-FE-4 |

## Blockers / Open Questions

- *(none)*
