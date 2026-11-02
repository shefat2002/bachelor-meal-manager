# Meal Manager Backend

ASP.NET Core Web API (.NET 10) + Orleans + EF Core/Npgsql on **Neon Postgres**, orchestrated locally with **ASP.NET Aspire**. See `AGENTS.md` in this folder and `docs/BACKEND_PROJECT_PLAN.md` for the full architecture.

## Solution layout

| Project | Purpose |
|---|---|
| `src/MealManager.Api` | Web API host — controllers, `/health` + `/health/ready`, Aspire service defaults |
| `src/MealManager.Application` | CQRS (MediatR + FluentValidation), per-feature `Features/<Domain>/{Commands,Queries}` |
| `src/MealManager.Infrastructure` | EF Core + Npgsql (Neon Postgres), repositories; `ApplicationContext` lands in Phase 1 |
| `src/MealManager.OrleansHost` | Placeholder for dedicated Orleans silo hosts (silo starts in the API process in Phase 3) |
| `src/MealManager.AppHost` | **Aspire AppHost** — local orchestration entry point |
| `src/MealManager.ServiceDefaults` | Shared Aspire service defaults (OTel, health checks, resilience) |
| `tests/MealManager.UnitTests` | xUnit unit tests |
| `tests/MealManager.IntegrationTests` | xUnit + `WebApplicationFactory` integration tests |

## Running locally

```bash
dotnet run --project src/MealManager.AppHost
```

Starts (dashboard URL printed to console):

- `api` — http://localhost:5080 (`/health`, `/health/ready`)
- `orleanshost` — placeholder worker
- `frontend` — Next.js dev server on http://localhost:3000 (runs `npm run dev` in `../frontend`)
- `redis` + `rabbitmq` (management UI on the dashboard) — **only when Docker is available**; the AppHost skips them with a warning otherwise (client wiring arrives in Phase 3/4)

Postgres is **never** run locally — the API connects to Neon (see below).

## One-time setup: Neon connection strings (user-secrets)

The AppHost refuses to start without the two Neon connection strings (they live in
user-secrets, never in git). Copy the values from the repo root `.env.local`:

```bash
POOLED=$(sed -n 's/^DATABASE_URL="\(.*\)"$/\1/p' ../.env.local)      # Neon pooled URL
UNPOOLED=$(sed -n 's/^DATABASE_URL_UNPOOLED="\(.*\)"$/\1/p' ../.env.local)  # Neon direct URL

dotnet user-secrets set "ConnectionStrings:messdb" "$POOLED" --project src/MealManager.AppHost
dotnet user-secrets set "ConnectionStrings:messdb-migrations" "$UNPOOLED" --project src/MealManager.AppHost
```

The **Api** project also needs its own secrets for standalone runs and EF tooling:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -hex 64)" --project src/MealManager.Api
dotnet user-secrets set "ConnectionStrings:messdb" "$POOLED" --project src/MealManager.Api
```

### Applying migrations to Neon

`dotnet ef database update --connection` mangles `=` in connection strings, and Npgsql
needs keyword format — so apply migrations by temporarily swapping the Api's `messdb`
secret to the converted **unpooled** URL:

```bash
UNPOOLED_CLEAN='Host=<host>;Port=5432;Username=<user>;Password=<pass>;Database=neondb;SSL Mode=Require'
dotnet user-secrets set "ConnectionStrings:messdb" "$UNPOOLED_CLEAN" --project src/MealManager.Api
dotnet ef database update --project src/MealManager.Infrastructure --startup-project src/MealManager.Api
dotnet user-secrets set "ConnectionStrings:messdb" "$POOLED" --project src/MealManager.Api   # restore
```

- `messdb` — **pooled** connection (app runtime; Npgsql through the Neon pooler)
- `messdb-migrations` — **direct/unpooled** (for `dotnet ef` migrations; DDL does not work over transaction pooling)

The AppHost injects both into the `api` service as `ConnectionStrings__messdb` / `ConnectionStrings__messdb-migrations`.

## Build & test

```bash
dotnet build
dotnet test
```

`global.json` pins the SDK (`10.0.403`, `rollForward: latestFeature`). If `dotnet` isn't installed:
`curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0` (installs to `~/.dotnet`).
