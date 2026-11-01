---
name: bmm-observability
description: Logging and monitoring setup for Bachelor Meal Manager — Serilog structured logging across all services, Prometheus /metrics endpoints, ASP.NET health checks, Grafana dashboards, and the docker-compose observability stack. Use when configuring logging, adding metrics or health checks, building dashboards, or diagnosing production issues via logs and metrics.
---

# Logging & Monitoring Configuration

Specs: `backend/AGENTS.md` (Observability), `docs/BACKEND_PROJECT_PLAN.md` §9.1. Every service logs via Serilog, exposes Prometheus metrics, and reports health — configured once, reused everywhere.

## Serilog

- Bootstrap on the host builder (`UseSerilog`) with **structured logging** — log properties, not interpolated strings:

```csharp
Log.Information("Bazar expense recorded {ExpenseId} for mess {MessId} amount {Amount}",
    expenseId, messId, amount);   // ✔ queryable properties
// NOT: Log.Information($"Recorded {expenseId}...");  // ✘ string interpolation kills structure
```

- Enrich every log with `RequestId`/correlation id + `MessId` (from tenant context) — one request is traceable across API → grain → worker.
- Sinks: console (dev, human format) + configurable sink for prod (file/Seq/Loki). Request logging middleware at Information; SQL/HTTP bodies at Debug only — never log tokens, passwords, or personal phone numbers.
- RabbitMQ consumers log message id + type at INFO on start/finish/failure (skill `bmm-rabbitmq-workers`).

## Prometheus metrics

- Expose `GET /metrics` (OpenTelemetry or prometheus-net): standard ASP.NET runtime + HTTP request duration histograms, plus custom counters that matter to this domain:
  - `bmm_meals_recorded_total`, `bmm_expenses_recorded_total` (per endpoint)
  - `bmm_summary_calculated_duration_seconds` (grain latency — the product's hot path)
  - `bmm_rabbitmq_messages_consumed_total{queue}`, `bmm_rabbitmq_message_failures_total{queue}`
- Health checks (`AddHealthChecks`): PostgreSQL connectivity, Redis ping, RabbitMQ connection, Orleans silo liveness — `/health` (liveness) and `/health/ready` (readiness, checks dependencies).

## Grafana

- Provisioned via docker-compose (config-as-code, `provisioning/dashboards` + datasources pointing at Prometheus) — no manually-clicked-only dashboards; JSON lives in the repo.
- Core dashboard panels: request rate + p95 latency, error rate, per-endpoint meal/expense throughput, grain summary latency, RabbitMQ queue depth + failures, DB/Redis health.

## Diagnosing with the stack

Production issue → Grafana (which signal spiked? errors? queue depth? grain latency?) → Serilog filtered by correlation id / MessId / time window → isolate request or consumer. When adding a feature, add its metric in the same PR — an unobservable path is unfinished.
