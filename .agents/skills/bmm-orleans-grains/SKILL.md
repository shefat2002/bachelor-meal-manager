---
name: bmm-orleans-grains
description: Microsoft Orleans virtual-actor implementation for Bachelor Meal Manager — MessGrain and MemberGrain design, grain storage backed by Redis/PostgreSQL, turn-based sequential concurrency for running meal/expense/deposit totals, and computing meal rate and balances inside grains. Use when implementing or changing grains, stateful financial calculations, silo configuration, or any concurrent stateful backend logic.
---

# Orleans Actor Management

Specs: `backend/AGENTS.md` (Architecture, Operational), `docs/BACKEND_PROJECT_PLAN.md` §5/§5.1. **All stateful calculations (meal rate, individual balances) run inside grains** — never compute them in controllers or ad-hoc queries (root `AGENTS.md` §4).

## Grain model

- **`IMessGrain`** (key: `Guid messId`) — the accounting brain of one mess: running monthly totals (meal units, bazar costs, utility costs, deposits collected). Exposes `GetSummary(month)` and record methods (`RecordMeals`, `RecordBazarExpense`, `RecordUtilityPayment`, `RecordDeposit`, `LockMonth`).
- **`IMemberGrain`** (key: `(Guid messId, Guid memberId)` — compound key) — one member's standing inside one mess: their meal-unit counter, deposits, computed cost/balance.
- Grains react to domain events after DB writes succeed (see `bmm-cqrs-mediatr`): the database is the record, grains are the sequential accumulator — so a grain can always be rebuilt from the event/DB history.

## Concurrency (the reason Orleans is here)

Orleans executes requests **per grain, one at a time (turn-based)** — no manual locks, no `lock` statements, no optimistic-concurrency exceptions for hot counters. Design so every mutation of a mess's running totals flows through the single `MessGrain` for that mess; concurrent meal updates then serialize naturally and totals stay correct.

- Keep turns short: no blocking I/O inside a grain turn; if enrichment is needed, await it outside or use `Task.Run`-free async calls that yield properly.
- Re-entrancy: enable `[Reentrant]` only where calls between grains are acyclic; prefer explicit call graphs (MessGrain → MemberGrain, never back).

## State & persistence

- Grain state persisted via configured storage — **Redis grain storage** for hot counters, PostgreSQL storage for durable summaries (plan §5.1: grains load from Redis/PostgreSQL on activation).
- `[RecoverableState]`-style pattern: on activation, if state is empty/stale, rebuild totals from the DB source of truth rather than erroring.
- Size guard: never let grain state grow unbounded — keep only running aggregates per active month; closed months live in the DB (summary snapshot written on month lock).

## Silo wiring

- Silo hosted in the API process initially (`UseOrleans` on the host builder) — clustering can move to separate silos later without grain code changes.
- Configure clustering (Redis-backed Adonis/ADO.NET or dev clustering for local), grain storage providers by name, and Dashboard (`AddServiceOrleansDashboard`) for local debugging.

## Testing

Unit-test grain logic with `TestKit`/`HostTesting` (call methods sequentially, assert accumulated totals), plus an integration test that hammers one mess with concurrent meal submissions and asserts the final totals equal the sum — this is the regression test for the whole point of using Orleans.
