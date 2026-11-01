# P0-IN-1: GitHub Actions CI skeleton

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Infra

## What was built

One workflow, two parallel independent jobs: **backend** (setup-dotnet pinned by `backend/global.json` → restore → build Release → test) and **frontend** (setup-node pinned by `frontend/.nvmrc` with npm cache → `npm ci` → lint → build). Triggers: push to `main` + PRs to `main`, with per-ref concurrency cancellation.

## Files touched

- `.github/workflows/ci.yml` — as above; job names stable ("Backend (build + test)", "Frontend (lint + build)") since they become commit statuses
- `frontend/.nvmrc` — `24.19.0` (installed Node; `@types/node` ^20 in package.json is only typing and can be bumped later)

## Key decisions & why

- **One `ci.yml`, two jobs** (the skill allows either) — single pipeline view, jobs parallel and independent.
- **No Docker/registry/deploy yet** — Phase 0 scope is build+test only; Phase 5 (P5-IN-1) adds docker build/push. Integration tests currently need no containers (WebApplicationFactory in-memory), so the runner needs no Docker.
- **Toolchain pins are files, not workflow literals** — `global.json` + `.nvmrc` keep local dev and CI identical.

## How to verify / test

Push to `main` and watch the Actions tab (verified post-push in this session's final step).

## Follow-ups / known gaps

- TestContainers-based integration tests (Phase 1+) will still work — ubuntu-latest runners have Docker.
