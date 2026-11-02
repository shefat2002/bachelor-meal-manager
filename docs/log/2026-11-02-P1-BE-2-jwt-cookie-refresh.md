# P1-BE-2: JWT endpoints — login/refresh/logout/me + cookie refresh

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Backend

## What was built

JWT auth wired end-to-end: 15-min HMAC access tokens (`Jwt:Key` from Api user-secrets), rotating refresh tokens delivered **only** via the HttpOnly `bmm_refresh` cookie (Path=/api/auth, SameSite=Lax, Secure outside dev, 7d), reuse detection (replaying a rotated token revokes every session of that user), CORS with credentials for localhost:3000, fallback authorization policy (everything authenticated unless `[AllowAnonymous]`).

## Files touched

- `Api/Controllers/AuthController.cs` — register(201)/login/otp/refresh/logout(204)/me; cookie set/clear helpers; refresh token never serialized
- `Application/Features/Auth/Commands/{RefreshTokenCommand,LogoutCommand}.cs` — rotation + best-effort revoke
- `Api/Program.cs` — JwtBearer (lazy options lambda!), CORS, error middleware, health split (live-tag `/health`; `/health/ready` = all incl. real Neon ping via `PostgresHealthCheck` with URI conversion)
- `Api/Middleware/PostgresHealthCheck.cs` — readiness probe opening a real Npgsql connection

## Key decisions & why

- **Lazy JwtBearer options lambda**: reading `Jwt:Key` at registration breaks in the test host (user-secrets + in-memory overrides apply at different times) — validation and TokenService must read the SAME final config.
- **Cookie over JSON refresh**: XSS-safe (skill bmm-app-router-multitenancy); `Path=/api/auth` keeps the cookie off other endpoints; cookies are port-agnostic on localhost so the Next middleware can read its presence.
- **Health split**: liveness must not fail when Neon blips; readiness includes it.

## How to verify / test

`AuthFlowTests` + live: register→`/me` 200; cookie refresh rotation; replay → 401 + all sessions revoked; logout → refresh 401. `/health/ready` → Healthy against Neon.

## Follow-ups / known gaps

- Token revocation list on the access token is accepted (15-min window) — standard tradeoff.
