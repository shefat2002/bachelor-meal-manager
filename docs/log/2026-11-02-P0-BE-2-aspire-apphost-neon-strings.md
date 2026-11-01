# P0-BE-2: Aspire AppHost + Neon connection strings

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Backend + Infrastructure

## What was built

ASP.NET Aspire (13.6.4) local orchestration: an AppHost that runs the API (fixed http://localhost:5080), the OrleansHost placeholder, and the Next.js dev server (:3000), with Redis + RabbitMQ (management UI) as container resources **when Docker is available** — skipped with a warning otherwise. Neon Postgres connection strings are declared as `messdb` (pooled) and `messdb-migrations` (unpooled) and injected into the API. Smoke-verified end to end on a Docker-less machine.

## Files touched

- `src/MealManager.AppHost/AppHost.cs` — orchestration model + `IsDockerAvailable()` guard
- `src/MealManager.AppHost/MealManager.AppHost.csproj` — packages `Aspire.Hosting.RabbitMQ`/`Aspire.Hosting.Redis` 13.6.4 + `Aspire.Hosting.NodeJs` 9.5.2; references Api + OrleansHost
- `src/MealManager.ServiceDefaults/` — Aspire template (OTel, health checks, resilience); referenced by Api + OrleansHost (`AddServiceDefaults()`)
- AppHost **user-secrets** — `ConnectionStrings:messdb` / `messdb-migrations` (values from root `.env.local`; never committed)
- `backend/README.md` — run instructions + one-time secrets setup commands
- Api/OrleansHost get `AddServiceDefaults()`; Api launch pinned to 5080 by the AppHost

## Key decisions & why

- **Docker-optional containers** — this dev machine has no Docker at all; failing hard would block all local runs. Client wiring for Redis/RabbitMQ only arrives in Phase 3/4, so skipping them is safe today. CI runners have Docker.
- **RabbitMQ API note**: Aspire 13 renamed `WithManagementInterface()` → `WithManagementPlugin()`.
- **Two connection strings, user-secrets on the AppHost** — pooled for app runtime, direct for EF migrations (DDL over PgBouncer fails); Aspire injects them into the api as `ConnectionStrings__*`. `AddConnectionString` throws at startup if secrets are missing (README documents setup).

## How to verify / test

```bash
dotnet run --project src/MealManager.AppHost
# → dashboard URL printed; curl http://localhost:5080/health → Healthy; http://localhost:3000 → 200
```

## Follow-ups / known gaps

- With Docker installed, redis/rabbitmq containers + management UI appear automatically; Phase 3/4 wire the clients (`AddRedisClient`/`AddRabbitMQ` integrations).
- Frontend gets the api URL via Aspire env injection (`services__api__http__0`); mapping it to `NEXT_PUBLIC_API_BASE_URL` is a P1 concern.
