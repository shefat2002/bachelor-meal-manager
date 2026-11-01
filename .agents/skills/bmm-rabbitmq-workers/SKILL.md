---
name: bmm-rabbitmq-workers
description: Asynchronous messaging for Bachelor Meal Manager with RabbitMQ — exchange/queue topology, published domain events, BackgroundService consumers for invitation emails, notifications, and monthly report generation, plus retry/dead-letter handling and idempotency. Use when adding background jobs, message publishing or consuming, email/notification sending, or PDF report generation.
---

# Asynchronous Worker Setup (RabbitMQ)

Specs: `backend/AGENTS.md` (Message Broker), `docs/BACKEND_PROJECT_PLAN.md` §7. Anything slow or external (email, PDF, push) runs **asynchronously via RabbitMQ** — API requests never wait on it.

## What flows through the broker

| Event / job | Producer | Consumer |
|---|---|---|
| `invitation.created` / `member.joinRequested` | Members feature | Notification worker (email alerts, approval prompts) |
| `member.approved`, `member.roleChanged` | Members feature | Notification worker |
| `report.monthlySettlement.requested` | Reports feature (manager triggers / month-end schedule) | Report worker: builds PDF statement |
| `report.monthlySettlement.completed` | Report worker | Notification worker → in-app/SSE "ready to download" |

## Topology & reliability conventions

- **Publisher**: topic exchange `bmm.events`; routing key = `feature.eventName` (`members.invitationCreated`). Publish **after the DB transaction commits** (in the MediatR handler's post-commit step — see skill `bmm-cqrs-mediatr`) so no phantom events for rolled-back writes. Use publisher confirms.
- **Queues**: durable, named `bmm.worker.<name>` (`bmm.worker.notifications`, `bmm.worker.reports`); each binds the events it needs. Dead-letter exchange `bmm.dlx` + DLQ per queue.
- **Consumers**: .NET `BackgroundService` per worker with `PrefetchCount` small (jobs are chunky). Retry with backoff (poison messages go to DLQ after N attempts — never infinite retry).
- **Idempotency**: consumers must tolerate redelivery — carry a `MessageId` and dedupe (processed-message table or Redis set). A re-sent invitation email is annoying; a double-generated settlement PDF is a bug.
- **Serialization**: JSON with a versioned envelope `{ messageId, occurredAt, type, payload }` — never break old consumers when evolving payloads.

## Report generation worker

- Consumes `report.monthlySettlement.requested`, reads the month's summary (grain/DB — skill `bmm-orleans-grains`), renders the master settlement (per-member meals, costs, deposits, final balances) to PDF, stores it (file storage or DB blob), records job status, publishes `...completed`.
- **Job status table/endpoint** backs the frontend notification flow (SSE/polling — skill `bmm-financial-viz-async-reports`): track `Pending → Running → Completed/Failed` with timestamps.

## Local dev

RabbitMQ runs in docker-compose (Phase 0 task); management UI on :15672. Add an integration test with the TestContainers broker for publish→consume round-trip, and always log message IDs at INFO with payload at DEBUG (see skill `bmm-observability`).
