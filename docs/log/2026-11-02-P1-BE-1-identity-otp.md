# P1-BE-1: ASP.NET Core Identity — phone accounts + OTP login

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Backend

## What was built

Phone-number Identity accounts on Neon Postgres: `Member : IdentityUser<Guid>` (UserName = normalized phone), OTP login for existing accounts (hashed 6-digit codes, 5-min expiry, attempt counter, 3-per-15-min rate limit), and password auth with lockout. Handlers run through MediatR with FluentValidation; errors surface via the shared `AppException` → `ApiErrorResponse` middleware.

## Files touched

- `Application/Domain/{Member,Mess,MemberMess,InviteLink,RefreshToken,OtpCode}.cs` — domain entities (+ `MessRole`, `MemberStatus` enums)
- `Application/Abstractions/{IApplicationContext,ITenantContext,ICurrentUser,IMembershipService,TokenService}.cs` — persistence/tenant/token contracts (TokenService implementation included)
- `Application/Common/{Phone,Pagination}.cs` — phone normalize/validate + `PaginatedResult<T>` envelope
- `Application/Features/Auth/**` — Register/Login/RequestOtp/VerifyOtp/Refresh/Logout/Me + validators + `AuthTokenIssuer`
- `Application/Exceptions/AppException.cs` + `Api/Middleware/ErrorHandlingMiddleware.cs` — API_CONVENTIONS §4 error mapping
- `Infrastructure/DependencyInjection.cs` — `AddIdentityCore<Member>` (+roles, SignInManager, lockout), DbContext registration
- `Api` user-secrets — `Jwt:Key` (random 64-hex), `ConnectionStrings:messdb` (pooled, runtime-converted)

## Key decisions & why

- **OTP without SMS**: code returned in JSON only in Development; RabbitMQ notification worker (Phase 4) will deliver for real.
- **No account enumeration**: unknown phone and wrong password return identical 401s; OTP request returns success for unknown phones.
- **Refresh tokens stored as SHA-256 hashes** with rotation + reuse detection (see P1-BE-2 log).

## How to verify / test

`dotnet test` — `AuthFlowTests` (register→login→refresh→logout, duplicate 409, bad phone 400 w/ field errors, OTP e2e) + live curl flow (see checklist).

## Follow-ups / known gaps

- SuperAdmin seeding (role exists; no seeded account — add dev seeder when needed).
