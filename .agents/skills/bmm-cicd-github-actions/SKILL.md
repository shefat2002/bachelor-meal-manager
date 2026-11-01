---
name: bmm-cicd-github-actions
description: CI/CD pipeline automation for Bachelor Meal Manager with GitHub Actions — workflows triggered on push to main that build .NET code, run unit and integration tests, build and push Docker images to a container registry, and build/deploy the Next.js frontend. Use when creating or modifying GitHub Actions workflows, Dockerfiles, release automation, or anything about build and deploy pipelines.
---

# CI/CD Pipeline Automation (GitHub Actions)

Specs: `backend/AGENTS.md` (CI/CD), `docs/BACKEND_PROJECT_PLAN.md` §9.2. Pipeline triggers on **push to `main`**; it must build, test, containerize, and publish.

## Pipeline shape

```
push to main
  └── job: backend
        checkout → setup-dotnet (SDK pinned in global.json)
        → dotnet restore → dotnet build -c Release --no-restore
        → dotnet test --no-build (unit + integration; integration via TestContainers
          so only Docker is needed — no hosted Postgres/Redis/RabbitMQ services)
        → docker build/push image (tag: sha + latest) to registry
  └── job: frontend (parallel)
        checkout → setup-node (version pinned) → npm ci
        → lint → typecheck → next build
        → (deploy step per hosting choice)
```

Rules:
- Jobs are **parallel** (backend ∥ frontend) and independent — one failing doesn't hide the other's result.
- `npm ci` (not `npm install`), pinned tool versions (`global.json` for .NET, `.nvmrc`/`setup-node` pin) — reproducible agents-included builds.
- Integration tests requiring PostgreSQL/Redis/RabbitMQ use **TestContainers** so the runner needs only Docker.
- Registry push uses repo secrets (`REGISTRY`, credentials) — never hardcode; images tagged with the commit SHA (immutable) plus `latest`.

## Dockerfiles

- **Backend**: multi-stage — `sdk` stage restores/builds/tests, `runtime` stage copies publish output; non-root user; `EXPOSE 8080`; healthcheck hitting `/health`.
- **Frontend**: multi-stage Node build → slim standalone `next start` (Next `output: 'standalone'`) image.
- Local parity comes from the **Aspire AppHost** (P0-BE-2: redis, rabbitmq containers + frontend dev server; Postgres is Neon — connection strings only, never a local container). See skills `bmm-efcore-multitenancy`, `bmm-rabbitmq-workers`, `bmm-observability`.

## Conventions

- Workflow YAML lives in `.github/workflows/` — `backend.yml`, `frontend.yml` (or one `ci.yml` with two jobs). Names show up as commit statuses; keep them stable once introduced.
- PRs to main run the same build+test jobs (no push/deploy) — nothing merges red.
- Secrets management: registry creds, deploy tokens via GitHub environment secrets; branch protection requires green checks before merge.
- If deploy automation to a cloud host is added later (Phase 5 task `P5-IN-1`), keep it a separate job gated on the test jobs, using an environment with required reviewers for production.
