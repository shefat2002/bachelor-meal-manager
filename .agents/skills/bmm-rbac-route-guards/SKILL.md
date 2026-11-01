---
name: bmm-rbac-route-guards
description: Role-based access control for the Bachelor Meal Manager frontend — Next.js Edge Middleware route protection, the reusable <Authorize roles={[...]}> wrapper component, the two-tier role model (global Super Admin; mess-scoped Meal Manager and Member), read-only member views, and gating of expense drawers, utility configuration, member approvals, and month locking. Use when any screen, action, or route depends on user role, permissions, or session checks.
---

# RBAC & Route Guards

Specs: `frontend/AGENTS.md` §4, `docs/BACKEND_PROJECT_PLAN.md` §4, `docs/SKILL_BUILDER.md` (frontend §5).

## Role model (two tiers)

| Role | Scope | Can |
|---|---|---|
| `SuperAdmin` | Global (system) | Everything, all messes; create/delete messes |
| `MealManager` | One mess (tenant) | Log/edit meals & expenses, utility types, deposits approval, invites, month lock |
| `Member` | One mess (tenant) | Read mess data, mark own meals, own deposits |

- A user's role is **per mess** (`MembersMesses` row) — `useCurrentMess().myRole` is the role in the *active* mess, not a global property.
- The backend is the enforcement authority (JWT claims + `X-Mess-Id`); frontend guards are UX only. Never hide what isn't also forbidden server-side.

## Route protection — Edge Middleware

`middleware.ts` verifies the session token for everything under `(dashboard)` and redirects to `/login` (with `?next=`) before rendering. Middleware checks **authentication** (valid token); fine-grained **authorization** (role-in-mess) is resolved by `MessProvider`/server components and by gating UI.

## `<Authorize>` wrapper

Reusable client component; conditionally renders children:

```tsx
<Authorize roles={['MealManager', 'SuperAdmin']}>
  <AddBazarButton />
</Authorize>
```

- Accepts role names; renders `null` (or an `fallback` prop like a read-only hint) when `myRole` isn't allowed.
- Use it for: expense entry drawers, utility type configuration, deposit approval toggles, member approval actions, invite generation, month-lock controls.
- Regular Members get **read-only views**: meal sheet cells locked (no steppers), no `+` actions, settlement table visible but no Lock/Export buttons.

## Rules

- Never implement role checks with scattered `if (user.role === ...)` comparisons in components — always `<Authorize>` or `useCurrentMess().myRole` via a helper (`can(role, action)` if the matrix grows).
- Unauthorized ≠ unauthenticated: unauthenticated → login redirect; authenticated-but-not-a-member of the mess → "no access to this mess" screen.
- Keep server components/middleware as the first gate; `<Authorize>` as the second. Both must agree.
