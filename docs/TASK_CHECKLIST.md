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
| **Current phase** | Phase 0 — Foundation & Scaffolding |
| **Last completed** | — (project initialized 2026-11-02) |
| **In progress** | — |
| **Next up (parallelizable)** | P0-BE-1, P0-FE-1, P0-IN-1 |
| **Blocked** | — |
| **Last updated** | 2026-11-02 by ZCode (initialization) |

---

## Phase 0 — Foundation & Scaffolding

- [ ] **P0-BE-1** Scaffold .NET 10 solution: `Api`, `OrleansHost`, `Application` (CQRS), `Infrastructure` (EF Core), test projects; `/health` endpoint — *no deps*
- [ ] **P0-BE-2** `docker-compose.yml` (PostgreSQL, Redis, RabbitMQ) + connection-string configuration — *no deps*
- [ ] **P0-FE-1** Scaffold Next.js App Router (TS strict) + Tailwind + Shadcn UI + ESLint/Prettier per `frontend/AGENTS.md` — *no deps*
- [ ] **P0-FE-2** Core libs: `lib/api-client.ts` (Axios), `lib/query-client.ts`, `lib/auth.ts` skeleton, env config — *depends: P0-FE-1*
- [ ] **P0-IN-1** GitHub Actions CI skeleton: backend build+test, frontend lint+build, on push to `main` — *depends: P0-BE-1, P0-FE-1*
- [ ] **P0-DX-1** Write `docs/API_CONVENTIONS.md`: response envelope, `PaginatedResult<T>`, error codes, filtering/sorting/pagination query params — *no deps*
- [ ] **P0-DX-2** Normalize legacy "Hostel/HostelId" wording in `docs/BACKEND_PROJECT_OVERVIEW.md` + backend section of `docs/SKILL_BUILDER.md` to Mess/MessId — *low priority, anytime*

## Phase 1 — Identity, Auth & Multi-Tenancy Core

- [ ] **P1-BE-1** ASP.NET Core Identity: phone-number accounts, password + OTP login — *depends: P0-BE-1/2*
- [ ] **P1-BE-2** JWT endpoints: login, refresh, logout; role claims (SuperAdmin, MealManager, Member) — *depends: P1-BE-1*
- [ ] **P1-BE-3** Mess entity + migrations + `POST/GET/PUT/DELETE /api/messes` (creator → Meal Manager) — *depends: P1-BE-2*
- [ ] **P1-BE-4** MembersMesses M2M + invite-link join + approval workflow + `PUT /api/members/{id}/role` — *depends: P1-BE-3*
- [ ] **P1-BE-5** EF Core global query filters by `MessId` + tenant-isolation integration tests — *depends: P1-BE-3*
- [ ] **P1-FE-1** `(auth)` route group: login/verify screens with RHF+Zod forms — *depends: P1-BE-2 (contract), P0-FE-1*
- [ ] **P1-FE-2** Token storage/refresh + Axios interceptors (Bearer + `X-Mess-Id` auto-attach) — *depends: P1-FE-1*
- [ ] **P1-FE-3** Dashboard app shell: sidebar, top utility bar, mess switcher, mobile bottom nav — *depends: P1-FE-2*
- [ ] **P1-FE-4** RBAC: `<Authorize roles={[...]}>` wrapper + middleware route protection — *depends: P1-FE-2*
- [ ] **P1-FE-5** Mess create/join UI + invite link handling + pending-approvals actions — *depends: P1-BE-4, P1-FE-3*

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

## Blockers / Open Questions

- *(none)*
