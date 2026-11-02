# P1-FE-5: Mess create/join UI, invite links & pending approvals

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Frontend

## What was built

P1-FE-5 across three surfaces: the `/dashboard` selector (create mess → you're its manager; join by pasting an invite token), the `/invite/[token]` page (auto-joins → "request sent, awaiting approval"), and the members page — pending-approvals banner (amber, manager-only) with one-click Approve/Reject, invite generator with copyable link + 7-day note, role menu (promote/demote/remove with server-error surfacing), and the member table (role badges, mono tabular-nums phones/dates).

## Files touched

- `src/app/(dashboard)/dashboard/page.tsx`, `src/app/invite/[token]/page.tsx`, `src/app/(dashboard)/dashboard/[messId]/members/page.tsx`
- `src/features/messes/{services,hooks}.ts` — API calls + TanStack hooks with `['messes', messId, 'members']` keys and invalidation

## Key decisions & why

- **Hooks invalidate both `['messes']` and members** on membership changes — the switcher counts and the members table share state.
- Invite link uses `window.location.origin + /invite/<token>` — QR rendering is a later polish (Phase 2 members pass).

## How to verify / test

Live: manager invites → open link as second user → pending banner → approve → both see the member table.

## Follow-ups / known gaps

- Self-leave UI from within a mess (currently only via remove); add in Phase 2 members polish.
