# AGENTS.md — Bachelor Meal Manager (Universal Meal Manager System)

This is the **entry point for every AI agent** working in this repository. Read this file first, then the specialized instructions for your work area. It exists so that any agent can bootstrap full project context without re-explaining the project from scratch.

## 1. What This Project Is

A multi-tenant SaaS platform ("Bachelor Meal Manager") for shared messes/hostels to manage daily meal participation, bazar (grocery) expenses, utility payments, deposits, and automated monthly financial settlement (meal rate, individual costs, surplus/arrear balances).

- **Backend**: ASP.NET Core Web API (.NET 10, C# 14) + Microsoft Orleans (virtual actors) + PostgreSQL **hosted on Neon** + Redis + RabbitMQ + Serilog/Prometheus/Grafana. See [backend/AGENTS.md](./backend/AGENTS.md).
- **Frontend**: Next.js (App Router) + TypeScript strict + Tailwind CSS + Shadcn UI + TanStack Query + Zustand + React Hook Form/Zod + Recharts. Consumes the backend API over HTTP only — **no direct database access**. See [frontend/AGENTS.md](./frontend/AGENTS.md).
- **Local development** is orchestrated with **ASP.NET Aspire** (AppHost runs the API, Orleans host, frontend dev server, and Redis/RabbitMQ containers); Postgres stays on Neon (connection strings only, no local Postgres container).

## 2. Repository Map

| Path | Purpose |
|---|---|
| `backend/` | .NET solution (API, Orleans silo/host, tests). Agent rules: `backend/AGENTS.md` |
| `frontend/` | Next.js app. Agent rules: `frontend/AGENTS.md` |
| `docs/` | All specifications. **Read before designing anything.** |
| `docs/BACKEND_PROJECT_PLAN.md` | Canonical backend plan: schema, roles, API endpoints, business equations |
| `docs/BACKEND_PROJECT_OVERVIEW.md` | Backend architecture summary |
| `docs/FRONTEND_DESIGN_OVERVIEW.md` | UI/UX spec: design tokens, app shell, screen layouts |
| `docs/PROJECT_PHASES.md` | High-level roadmap: all phases and their goals |
| `docs/TASK_CHECKLIST.md` | **Single source of truth for current progress.** Update after every task |
| `docs/log/` | One markdown log per completed unit of task (format: `docs/log/README.md`) |
| `neon.ts` + `.env.local` (gitignored) | Neon project config (Postgres-only) + DB connection strings. Consumed by the **backend**; the frontend never reads them |
| `.agents/skills/` | 12 project skills (6 frontend, 6 backend) + 3 Neon Postgres reference skills (`neon`, `neon-postgres`, `neon-postgres-branches`). Use them |

## 3. Multi-Agent Workflow Protocol (mandatory)

This repo is developed by multiple AI agent sessions. To avoid re-explaining the project and to prevent conflicting work:

1. **On session start**: read `docs/TASK_CHECKLIST.md` (current status snapshot + next available tasks). Read the `docs/log/` entries for any task you will build upon. Only then read the detailed specs you need.
2. **Before working**: pick tasks from the checklist that are unblocked (their `Depends on` tasks are `[x]`). Do not start a task another agent marked in-progress unless the checklist says it was abandoned.
3. **While working**: follow the matching skill in `.agents/skills/` (they encode the project's conventions for routing, CQRS, tenancy, forms, etc.) and the area's `AGENTS.md`.
4. **After completing a unit of task**:
   - Tick the task in `docs/TASK_CHECKLIST.md` and update the **Status Snapshot** block at the top (current phase, last completed, next up, date).
   - Write a log file `docs/log/YYYY-MM-DD-<TASK-ID>-<short-slug>.md` describing what changed, files touched, and decisions made (template in `docs/log/README.md`).
   - Append any durable decision to the **Decisions Log** section of `docs/TASK_CHECKLIST.md`.
5. **Never** rewrite history in the checklist or delete log files — append and update status only.

## 4. Canonical Decisions (binding for all agents)

- **Tenant key is `MessId`** everywhere (DB columns, API routes, frontend routes, headers). Some docs historically used `HostelId`/`Hostels` — that wording was normalized to Mess/MessId on 2026-11-02 (P0-DX-2). Do not introduce `HostelId` in new code.
- **Database = Neon-hosted PostgreSQL, connected from the backend only.** EF Core/Npgsql lives in the .NET backend; use the pooled connection string (`DATABASE_URL`) for the app and the direct/unpooled one (`DATABASE_URL_UNPOOLED`) for EF migrations (both in root `.env.local`). The frontend has **no database access** — never add a DB driver, migration, or credential under `frontend/`.
- **Neon is used only for Postgres.** No Neon Auth / Realtime / Functions — identity is ASP.NET Core Identity + JWT (Phase 1), async UI updates arrive via the backend API.
- **Local dev orchestration = ASP.NET Aspire.** The AppHost (in the backend solution) runs the API, Orleans host, the Next.js dev server, and Redis/RabbitMQ as container resources. There is no docker-compose for app infrastructure; only the Phase 4 observability stack (Prometheus/Grafana) may use compose.
- **`frontend/` is the single npm root** — there is no root `package.json`; run all npm commands from `frontend/`.
- **Tenant header**: frontend attaches `X-Mess-Id: <id>` to all tenant-scoped API calls.
- **Roles**: `Super Admin` (global), `Meal Manager` (mess-scoped manager), `Member` (read-mostly; joins via invite requiring approval).
- **Financial equations** (from `docs/BACKEND_PROJECT_PLAN.md` §5):
  - Meal Rate = Total Meal Costs for month ÷ Total Meal Units consumed
  - Individual Meal Cost = Member's Meal Units × Meal Rate
  - Total Individual Cost = Individual Meal Cost + Share of Utility Costs
  - Surplus/Arrear = Total Money Given by Member − Total Individual Cost
  - On-Hand Balance = Total Money Collected − Total Expenses
- **All stateful calculations** (meal rate, balances) run inside Orleans grains — never compute them ad hoc in controllers.
- **CQRS everywhere**: MediatR commands/queries + FluentValidation; EF Core repositories with global query filters scoped by `MessId`.
- **Tests are part of Definition of Done** for every backend feature (unit + integration) and for critical frontend flows.

## 5. Common Commands

Backend (run from `backend/`):
```bash
dotnet build
dotnet test
dotnet run --project src/MealManager.AppHost   # ASP.NET Aspire: API + Orleans + frontend dev server + Redis/RabbitMQ containers
```

Frontend (run from `frontend/` — the single npm root; there is no root `package.json`):
```bash
npm install
npm run dev
npm run lint && npm run build
```

PostgreSQL is **not** run locally — the backend connects to **Neon** (connection strings in root `.env.local`; `neon.ts` at the repo root is the Neon project config for the `neon` CLI).

## 6. Definition of Done (any task)

- Code follows the applicable skill conventions and area `AGENTS.md`.
- Builds clean; tests written and passing.
- `docs/TASK_CHECKLIST.md` updated + `docs/log/` entry written.
- No `HostelId` terminology introduced; multi-tenant isolation respected.
