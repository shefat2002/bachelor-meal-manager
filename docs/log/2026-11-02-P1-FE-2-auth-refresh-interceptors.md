# P1-FE-2: Token storage, refresh & interceptors — completed

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Frontend

## What was built

The P0 skeletons became real: `lib/auth.ts` now performs the single-flight refresh against `POST /api/auth/refresh` (raw `fetch` + credentials, updates the in-memory access token + cached user), and `lib/auth-context.tsx` adds `AuthProvider`/`useAuth()` (session restore on mount via silent refresh, session-expired → `/login?next=`). The Axios `apiClient` gained `withCredentials` (HttpOnly cookie storage cross-origin) and its 401 interceptor now replays after a real refresh.

## Files touched

- `src/lib/auth.ts`, `src/lib/auth-context.tsx`, `src/lib/api-client.ts`, `src/app/providers.tsx` (ThemeProvider + AuthProvider + QueryClient)

## Key decisions & why

- **Refresh via `fetch`, not axios** — the interceptor must not consume the very 401 path it guards (no self-interception loops).
- **`withCredentials: true` globally** — register/login responses must be allowed to SET the bmm_refresh cookie cross-origin; backend CORS has AllowCredentials.

## How to verify / test

Build green; manual: hard-reload `/dashboard/[messId]` after login — session restores without a login redirect.

## Follow-ups / known gaps

- none for Phase 1.
