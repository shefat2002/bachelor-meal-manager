# P1-FE-1: (auth) route group — login/register/verify

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Frontend

## What was built

The `(auth)` group per the skill's route tree: `/login` (phone+password, `?next=` redirect), `/register` (name/phone/password/confirm), `/verify` (OTP request + verify; dev code surfaced when the API returns it). All forms are React Hook Form + Zod with schemas mirroring the backend FluentValidation rules 1:1 (`features/auth/schemas.ts` — phone normalize→regex, password ≥8, 6-digit code). Centered card layout via `(auth)/layout.tsx`.

## Files touched

- `src/app/(auth)/{layout,login/page,register/page,verify/page}.tsx`
- `src/features/auth/schemas.ts`

## Key decisions & why

- **`useSearchParams` inside `<Suspense>`** — cacheComponents rejects dynamic reads during prerender.
- **Errors from the API envelope**: field errors (`errors.password[0]`) are preferred over generic `detail` toasts.

## How to verify / test

`npm run build` (all three static); manual flow against the live API.

## Follow-ups / known gaps

- Login page could pre-fill phone from OTP entry; polish later.
