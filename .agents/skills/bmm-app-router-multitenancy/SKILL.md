---
name: bmm-app-router-multitenancy
description: Build Next.js App Router routes and multi-tenant mess scoping for Bachelor Meal Manager — route groups (auth)/(dashboard), [messId] tenant routes, MessContext / useCurrentMess hook, Axios interceptors attaching X-Mess-Id and JWT Bearer headers, and token refresh flows. Use whenever touching frontend routing, mess switching, tenant context, API client wiring, or auth token handling in this project.
---

# Next.js App Router & Multi-Tenant Mess Scoping

Full specs: `frontend/AGENTS.md` (§2, §3, §4.1) and root `AGENTS.md` §4 (canonical decisions). This skill is the operational summary.

## Route structure (do not invent alternatives)

```
src/app/
├── (auth)/                  # public: login, verify — no tenant context here
│   ├── login/page.tsx
│   └── layout.tsx
├── (dashboard)/
│   ├── layout.tsx           # injects sidebar/topbar, MessProvider, guards
│   └── [messId]/            # THE tenant boundary — everything scoped lives below
│       ├── page.tsx         # overview dashboard
│       ├── meals/
│       ├── expenses/
│       ├── members/
│       ├── reports/
│       └── settings/
├── layout.tsx               # global providers (QueryClient, AuthProvider)
└── page.tsx                 # landing / smart redirect to last active mess
```

Rules:
- The `[messId]` segment is the **only** tenant scope. Never carry `messId` in query strings or local state as the source of truth — the URL is bookmarkable/shareable and drives everything.
- `/dashboard/[messId]/*` views must survive a hard reload (session + tenant resolved server-side/middleware before render).

## MessContext

Create one provider in `(dashboard)/layout.tsx` exposing `useCurrentMess()`:

- Reads `messId` from route params, fetches the mess descriptor + **current user's role in that mess**.
- Exposes `{ messId, mess, myRole, isLoading }` — consumers never parse params themselves.
- Throwing or redirecting when the user is not a member of that mess is the provider's job, not each page's.

## Axios interceptors (`src/lib/api-client.ts`)

Two concerns, in this order:

1. **Request**: attach `Authorization: Bearer <accessToken>` and, when a mess is active, `X-Mess-Id: <messId>` — pulled from the current route, not from state that can go stale.
2. **Response 401**: queue the failed request, call the refresh endpoint once, replay; on refresh failure, clear session and redirect to `(auth)/login`. Guard against refresh loops (single-flight refresh promise).

```ts
apiClient.interceptors.request.use((config) => {
  config.headers.Authorization = `Bearer ${getToken()}`;
  const messId = getActiveMessId(); // from route, set by MessProvider
  if (messId) config.headers['X-Mess-Id'] = messId;
  return config;
});
```

## Mess switching

Switching mess = navigating to the new `/dashboard/[newMessId]/...` route. The MessProvider change must:
- Invalidate all `['messes', oldMessId, ...]` TanStack Query caches (see skill `bmm-server-state`) so stale tenant data never renders.
- Reset any per-mess Zustand UI state (open drawers, filters).

## Gotchas

- Access token lives in memory; refresh token in a secure HTTP-only cookie — never `localStorage` for tokens (XSS).
- Tenant-scoped API calls outside the dashboard (e.g. invite acceptance in `(auth)`) must attach `X-Mess-Id` explicitly.
- Route params in App Router are strings — validate `messId` shape (guid) before firing requests.
