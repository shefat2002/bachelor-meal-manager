# Project Phases & High-Level Overview

> Companion to `docs/TASK_CHECKLIST.md` (which tracks execution status task-by-task).
> This file explains **what the phases are and why**; the checklist defines the concrete tasks and their state.

## High-Level Overview

The Universal Meal Manager System is a multi-tenant platform where many messes (shared kitchens in hostels/bachelor apartments) operate independently but on shared infrastructure:

- **Members** mark daily meals (breakfast/lunch/dinner), record bazar (grocery) purchases, and pay deposits.
- **Meal Managers** (per mess) approve joins, configure utility types, log utility bills, verify entries, and close the month.
- **Super Admins** manage the platform and create/delete messes.
- The system continuously computes **meal rate, individual costs, surplus/arrear balances, and on-hand cash**, and produces monthly settlement statements (PDF) via background jobs.

### Architecture in one paragraph

An ASP.NET Core Web API fronted by CQRS (MediatR + FluentValidation) writes to PostgreSQL via EF Core with global tenant filters (`MessId`). Hot, sequential-per-entity state (running meal totals, balances) lives in Orleans grains persisted to Redis/PostgreSQL. RabbitMQ decouples slow side effects (emails, monthly report generation). Serilog + Prometheus + Grafana observe everything, GitHub Actions builds/tests/ship Docker images. The Next.js frontend consumes the API with TanStack Query, scopes every request to the active mess (`/dashboard/[messId]/*` + `X-Mess-Id` header), and renders financial data with strict numeric design rules.

### Parallel tracks

Backend (`BE-*`) and frontend (`FE-*`) tasks run as parallel tracks inside each phase. Frontend tasks in a phase generally depend on that phase's backend API tasks, so **backend leads each phase slightly**. Infra/docs tasks (`IN-*`) support both.

---

## Phase 0 — Foundation & Scaffolding

**Goal**: runnable skeletons + local infrastructure so all later work happens on stable ground.

- .NET solution layout (Api, Orleans host, Application/Core, Persistence, Tests) and Next.js scaffold with strict TypeScript, Tailwind, Shadcn UI.
- `docker-compose.yml` for PostgreSQL, Redis, RabbitMQ (+ Prometheus/Grafana later in Phase 4).
- CI skeleton on GitHub Actions (build + test on push to `main`).
- Conventions land here: base response envelopes, pagination model, error contract, lint/format rules.

**Exit criteria**: `docker compose up -d` + `dotnet run` + `npm run dev` all succeed; CI green on a trivial change.

## Phase 1 — Identity, Auth & Multi-Tenancy Core

**Goal**: users can register/login (phone + password/OTP), receive JWT access/refresh tokens, and the tenant model exists.

- ASP.NET Core Identity + JWT issuance/refresh; role model (Super Admin, Meal Manager, Member).
- Mess entity + CRUD (`POST/GET/PUT/DELETE /api/messes`) with creator becoming Meal Manager.
- Members↔Messes join/leave, invitation links with approval workflow.
- EF Core global query filters by `MessId`; tenant-isolation integration tests.
- Frontend: `(auth)` route group (login/verify), token storage & refresh interceptors, dashboard shell with mess switcher, `X-Mess-Id` injection, RBAC `<Authorize>` guard + middleware.

**Exit criteria**: two messes can exist with zero data leakage between them; a member can join via invite and be approved.

## Phase 2 — Core Domain: Meals, Bazar & Utilities

**Goal**: the three daily data streams are captured and queryable.

- `POST/GET /api/meals` (per member/day: breakfast, lunch, dinner; filtering, sorting, pagination).
- `POST/GET /api/expenses/bazar` (item, price, date, buyer).
- UtilityTypes config (per mess, manager-defined) + `POST/GET /api/expenses/utility`.
- Member deposits ledger with manager approval.
- Frontend: meal sheet (daily entry view + monthly matrix), expenses hub (bazar/utility/deposits tabs), member directory & invites UI.

**Exit criteria**: a mess can log a full day of meals, expenses, and deposits through the web UI.

## Phase 3 — Financial Logic: Orleans, Summary & Settlement

**Goal**: the heart of the product — always-correct money math.

- Orleans grains for Mess and Member (running totals, sequential concurrency, persisted state).
- `GET /api/summary/messes/{messId}`: meal rate, per-member costs, surplus/arrear (equations in `docs/BACKEND_PROJECT_PLAN.md` §5).
- Redis caching of summary/meal-rate; invalidation on writes.
- Month lock (freeze edits after settlement).
- Frontend: overview dashboard KPIs, trend charts (Recharts), master settlement table, my-status card.

**Exit criteria**: summary matches hand-computed values on a seeded dataset; concurrent meal updates never produce a wrong total.

## Phase 4 — Async Jobs, Notifications & Observability

**Goal**: the system works in the background and is observable in production.

- RabbitMQ topology + background workers: invitation emails/notifications, monthly settlement report generation (PDF).
- SSE/WebSocket or polling endpoint for job-status notifications.
- Serilog structured logging, Prometheus metrics endpoint, Grafana dashboards, health checks.
- Frontend: notification popover, report-ready alert + download flow.

**Exit criteria**: monthly report is generated asynchronously and the manager is notified in-app; Grafana shows API health live.

## Phase 5 — Hardening, CI/CD & Release

**Goal**: production-grade.

- Full GitHub Actions pipeline: build → test → Docker image → container registry push (backend), frontend build/deploy.
- Test hardening: unit + integration coverage across domains; multi-tenant isolation test suite; critical frontend flows.
- Security pass: token expiry/refresh edge cases, authorization on every endpoint, rate limiting, input fuzzing on validators.
- Seeding/demo data, READMEs, deployment docs.

**Exit criteria**: one-command deployment from a clean checkout; all tests green in CI; demo mess fully exercisable end-to-end.

---

## Phase dependency graph

```
Phase 0 ──► Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4 ──► Phase 5
 (scaffolds)  (auth+     (domain     (money      (async +     (ship)
               tenancy)    capture)    math)       observ.)
```

Within a phase, `BE-*` tasks should complete (or at least their API contracts freeze) before the dependent `FE-*` tasks start.
