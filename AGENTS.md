# AGENTS.md — Bachelor Meal Manager (Universal Meal Manager System)

This is the **entry point for every AI agent** working in this repository. Read this file first, then the specialized instructions for your work area. It exists so that any agent can bootstrap full project context without re-explaining the project from scratch.

## 1. What This Project Is

A multi-tenant SaaS platform ("Bachelor Meal Manager") for shared messes/hostels to manage daily meal participation, bazar (grocery) expenses, utility payments, deposits, and automated monthly financial settlement (meal rate, individual costs, surplus/arrear balances).

- **Backend**: ASP.NET Core Web API (.NET 10, C# 14) + Microsoft Orleans (virtual actors) + PostgreSQL + Redis + RabbitMQ + Serilog/Prometheus/Grafana. See [backend/AGENTS.md](./backend/AGENTS.md).
- **Frontend**: Next.js (App Router) + TypeScript strict + Tailwind CSS + Shadcn UI + TanStack Query + Zustand + React Hook Form/Zod + Recharts. See [frontend/AGENTS.md](./frontend/AGENTS.md).

## 2. Repository Map

| Path | Purpose |
|---|---|
| `backend/` | .NET solution (API, Orleans silo/host, tests). Agent rules: `backend/AGENTS.md` |
| `frontend/` | Next.js app. Agent rules: `frontend/AGENTS.md` |
| `docs/` | All specifications. **Read before designing anything.** |
| `docs/BACKEND_PROJECT_PLAN.md` | Canonical backend plan: schema, roles, API endpoints, business equations |
| `docs/BACKEND_PROJECT_OVERVIEW.md` | Backend architecture summary (uses older "Hostel" wording — see §4) |
| `docs/FRONTEND_DESIGN_OVERVIEW.md` | UI/UX spec: design tokens, app shell, screen layouts |
| `docs/PROJECT_PHASES.md` | High-level roadmap: all phases and their goals |
| `docs/TASK_CHECKLIST.md` | **Single source of truth for current progress.** Update after every task |
| `docs/log/` | One markdown log per completed unit of task (format: `docs/log/README.md`) |
| `.agents/skills/` | 12 project skills (6 frontend, 6 backend) that encode how to build each concern. Use them |

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

- **Tenant key is `MessId`** everywhere (DB columns, API routes, frontend routes, headers). Older docs (`docs/BACKEND_PROJECT_OVERVIEW.md`, backend sections of `docs/SKILL_BUILDER.md`) still say `HostelId`/`Hostels` — treat those as legacy wording for `MessId`/`Messes`. Do not introduce `HostelId` in new code.
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
dotnet run --project src/MealManager.Api   # adjust to actual solution layout once scaffolded
```

Frontend (run from `frontend/`):
```bash
npm install
npm run dev
npm run lint && npm run build
```

Local infrastructure (PostgreSQL, Redis, RabbitMQ, Prometheus, Grafana) is dockerized — use `docker compose up -d` once `docker-compose.yml` exists (Phase 0 task).

## 6. Definition of Done (any task)

- Code follows the applicable skill conventions and area `AGENTS.md`.
- Builds clean; tests written and passing.
- `docs/TASK_CHECKLIST.md` updated + `docs/log/` entry written.
- No `HostelId` terminology introduced; multi-tenant isolation respected.
