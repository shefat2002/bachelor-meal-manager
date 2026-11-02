# P2-FE-5: Members directory polish — QR invite + self-leave

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 frontend session)
- **Phase / track**: Phase 2 / Frontend

## What was built
Polished the Phase 1 members page: the invite link now renders in a Dialog with a **QR code** (`qrcode.react`, white-backed tile for scannability), the full link, and copy-to-clipboard — replacing the inline code card. Added **Leave this mess** (self-leave, confirm prompt → `DELETE /api/members/{selfId}` → redirect to `/dashboard`); the backend already allowed self-removal (P1 `RemoveMemberCommand` isSelf path), so no API change was needed. Role badges and the pending-approvals banner shipped in P1-FE-5 and were left as-is.

## Files touched
- `frontend/src/app/(dashboard)/dashboard/[messId]/members/page.tsx` — invite Dialog + QR, self-leave handler + footer button.
- `frontend/package.json` — new dep `qrcode.react` (QR rendering is frontend-only, per the P1-BE-4 log note).
- `frontend/src/components/ui/dialog.tsx` already existed; no new primitives.

## Key decisions & why
- **QR in a Dialog, not inline** — the invite flow is transient; a modal keeps the directory clean (design overview §3.5 "Invite Modal: link or QR code").
- **Self-leave via the existing remove endpoint** (`isSelf` branch, last-manager guard still applies → 409 surfaces as a toast) — zero backend churn.
- Leave button styled `text-arrear` outline — destructive intent without hiding it in a menu.

## How to verify / test
- Manager: Invite member → Dialog shows QR + link; scan with a phone to confirm it opens `/invite/<token>`.
- Any member: Leave this mess → confirm → lands on `/dashboard`; rejoining needs a new invite.

## Follow-ups / known gaps
- Invite expiry countdown not implemented (static "valid 7 days" text matches backend TTL); polish later if users complain.
