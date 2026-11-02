# P1-FE-3: Dashboard app shell

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Frontend

## What was built

The design-spec §2 shell: collapsible desktop sidebar (Overview / Meal Sheet / Expenses & Deposits / Members / Reports + manager-gated Settings), top utility bar (active-month switcher backed by the zustand UI store, theme toggle, user menu with sign-out), and the mobile bottom nav (Overview/Meals/Expenses/Summary). `MessProvider` (`components/mess-provider.tsx`) validates the GUID, fetches mess + myRole, installs the api-client mess-id source, drops old-mess caches on switch, and renders the no-access screen. `/dashboard` is the mess selector (list + create + join-by-token).

## Files touched

- `src/components/{app-shell,mess-provider}.tsx`, `src/lib/stores/ui.ts`
- `src/app/(dashboard)/dashboard/page.tsx`, `src/app/(dashboard)/dashboard/[messId]/layout.tsx` + section pages (overview/meals/expenses/reports placeholders, settings rename, members real)

## Key decisions & why

- **Shell at `(dashboard)/dashboard/[messId]/layout.tsx`** (deviation from the skill's `(dashboard)/layout.tsx`): the sidebar/switcher need the active mess, which only exists under `[messId]`; the selector page gets no tenant shell. The route GROUP still provides the boundary.
- **cacheComponents**: `await params` happens inside `<Suspense>` (async `TenantGate`).

## How to verify / test

`npm run build` (PPR for [messId] routes); login → pick mess → shell renders, month switcher works, dark mode toggles.

## Follow-ups / known gaps

- Notification popover + Quick Entry button arrive Phase 4; KPI cards Phase 3.
