# P1-FE-4: RBAC — middleware protection + <Authorize>

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Frontend

## What was built

Two-tier guards per skill bmm-rbac-route-guards: Edge `middleware.ts` protects `/dashboard/*` and `/invite/*` by the presence of the `bmm_refresh` cookie (redirect `/login?next=`), and the `<Authorize roles={[...]} fallback>` wrapper gates UI on `useCurrentMess().myRole` (SuperAdmin global role wins). Settings page and all manager actions (invite/approve/role/remove) are wrapped.

## Files touched

- `src/middleware.ts`, `src/components/authorize.tsx`, settings/members pages (usage)

## Key decisions & why

- **Middleware checks authentication only** — the cookie's presence; role-in-mess is MessProvider's job; the backend is the enforcement authority (frontend guards are UX).
- **Unauthorized ≠ unauthenticated**: non-member of a mess gets the no-access screen, not a login redirect.

## How to verify / test

Built-app smoke: `/dashboard` → 307 `/login?next=%2Fdashboard`; `/invite/x` → 307. Member account sees no Settings/invite buttons.

## Follow-ups / known gaps

- none for Phase 1.
