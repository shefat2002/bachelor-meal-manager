# P1-BE-4: MembersMesses M2M, invites, approval & roles

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 1 / Backend

## What was built

The full membership lifecycle behind `X-Mess-Id`: manager-minted single-use invite tokens (7d), join → **Pending** membership, manager approve/reject, role assignment (`PUT /api/members/{id}/role`), leave/remove, all guarded by "at least one active Meal Manager" (409). Tenant-scoped listing with pending visible to managers only.

## Files touched

- `Application/Features/Members/**` — CreateInvite/Join/List/Approve/Reject/UpdateRole/Remove + contracts
- `Api/Controllers/MembersController.cs`

## Key decisions & why

- **Join is NOT tenant-scoped** — the joiner doesn't know the mess id; the invite token resolves it (skill's tenant-scoping still applies everywhere else via `ITenantRequest`).
- **Invite tokens single-use** — regeneration is one click; prevents link sprawl. Expired/used → 409, unknown → 404.
- **Last-manager guard** shared helper (`UpdateMemberRoleCommandHandler.GuardLastManagerAsync`) — used by role change AND leave/remove.
- Handlers inject `IApplicationContext` directly (repository layer deferred; CQRS + tenant pipeline are the load-bearing conventions).

## How to verify / test

`MessMemberFlowTests.Full_Mess_Lifecycle_WithTenantRules` (invite→join 201→double-join 409→pending→approve 204→member list 200→member invite 403→promote→demote→delete) + live curl mirror.

## Follow-ups / known gaps

- QR code rendering is frontend-only (Phase 2 members page polish); email notifications on invite/approve arrive with RabbitMQ (Phase 4).
