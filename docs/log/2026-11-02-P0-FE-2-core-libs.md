# P0-FE-2: Frontend core libs (api-client, auth, query-client, env)

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Frontend

## What was built

The four `src/lib` modules every feature builds on, plus global providers: an Axios client with JWT/tenant interceptors and single-flight 401 refresh-replay, an in-memory token store, a TanStack Query client factory with the project's retry policy, typed env config, and `Providers` (QueryClientProvider + Toaster) mounted in the root layout.

## Files touched

- `src/lib/env.ts` — `NEXT_PUBLIC_API_BASE_URL`, default `http://localhost:5080` (Aspire AppHost's fixed api port)
- `src/lib/auth.ts` — `getAccessToken/setAccessToken/clearSession`, `onSessionExpired`, `refreshAccessToken()` stub (returns null until P1-FE-2 wires `POST /api/auth/refresh`); no localStorage, ever
- `src/lib/api-client.ts` — request interceptor: `Authorization: Bearer` + `X-Mess-Id` from `setActiveMessIdSource()` (MessProvider installs the route-derived source in P1); response interceptor: 401 → single-flight refresh (`_retry` guard against loops) → replay or `clearSession()` + `notifySessionExpired()`
- `src/lib/query-client.ts` — `makeQueryClient()`: staleTime 30s, retry ≤2 with backoff, never retry 4xx
- `src/app/providers.tsx` + `src/app/layout.tsx` — QueryClientProvider + sonner Toaster around the app
- `package.json` — deps axios + @tanstack/react-query

## Key decisions & why

- **Mess-id source is injected, not imported** — the interceptor stays route-agnostic until `[messId]` routing exists (P1-FE-3), per skill `bmm-app-router-multitenancy` ("from the current route, not state that can go stale").
- **Refresh is a stub that fails closed** — until the refresh endpoint exists (P1-BE-2), a 401 clears the session instead of looping.

## How to verify / test

```bash
cd frontend && npm run lint && npm run build
```

## Follow-ups / known gaps

- P1-FE-2 wires real refresh + token events; query keys/hooks conventions land with the first features (`bmm-server-state`).
