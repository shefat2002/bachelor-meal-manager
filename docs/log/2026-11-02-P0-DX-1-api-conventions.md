# P0-DX-1: docs/API_CONVENTIONS.md

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Docs

## What was built

The binding API contract every backend endpoint and the frontend `types/api.ts` mirror 1:1: success envelopes (direct DTO / `PaginatedResult<T> { items, page, pageSize, totalCount }`), list query params (`page`, `pageSize` ≤ 100, whitelisted `sortBy`, `sortDir`, `from`/`to` as `YYYY-MM-DD`), the ProblemDetails-shaped `ApiErrorResponse` with a stable error-code table, header contract (`Authorization` + `X-Mess-Id`), data-type rules (decimal money, 0.5-step meal counts, GUID ids, camelCase), and the endpoint catalogue cross-referenced to `docs/BACKEND_PROJECT_PLAN.md` §6.

## Files touched

- `docs/API_CONVENTIONS.md` — new (referenced previously by skill `bmm-cqrs-mediatr` as "envelope defined in docs/API_CONVENTIONS.md, Phase 0")

## Key decisions & why

- **Error codes as `type` strings** (VALIDATION_ERROR, TENANT_REQUIRED, UNAUTHENTICATED, FORBIDDEN, NOT_FOUND, CONFLICT, RATE_LIMITED, INTERNAL_ERROR) — the frontend switches on codes, never on prose; `NOT_FOUND` also masks cross-tenant existence.
- **`errors` maps field paths → messages** straight from the FluentValidation pipeline (camelCased), matching the 400 short-circuit contract in `bmm-cqrs-mediatr`.
- **Dates `YYYY-MM-DD`, money as decimal numbers, enums camelCase strings** — fixed now so P1 contracts don't drift.

## How to verify / test

Review against `docs/BACKEND_PROJECT_PLAN.md` §6 (catalogue matches); P1+ endpoints must comply.

## Follow-ups / known gaps

- Per-endpoint sort whitelists and extra filters get appended here in the same PRs that add them.
