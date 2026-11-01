---
name: bmm-design-system-app-shell
description: Design system and responsive app shell conventions for Bachelor Meal Manager — Tailwind CSS + Shadcn UI (Radix) primitives, semantic color tokens (indigo primary, emerald surplus, rose arrear, amber pending), tabular-nums numeric typography, light/dark themes, collapsible desktop sidebar with mess switcher, and mobile bottom navigation. Use when building or styling any screen, component, badge, table, or layout in the frontend.
---

# Design System & Responsive App Shell

Canonical spec: `docs/FRONTEND_DESIGN_OVERVIEW.md` (§1, §2). Aesthetic: clean utilitarian SaaS (Linear/Stripe vibe) — high contrast, minimal clutter. When in doubt, less chrome, more data density.

## Design tokens (fixed — do not drift)

| Token | Value | Used for |
|---|---|---|
| Primary / action | `indigo-600` | Buttons, active nav, links |
| Surplus / positive | `emerald-600` text on `emerald-50` badge | Member deposited ≥ cost |
| Arrear / due | `rose-600` text on `rose-50` badge | Member owes money |
| Warning / pending | `amber-600` | Unapproved joins, unverified entries |
| Dark background | `slate-950` | Dark mode |
| Light background | `white` / `slate-50` | Light mode |

Rules:
- **Surplus/arrear/pending are semantic**, never decorative. A red badge means "owes money", nothing else.
- First-class light AND dark mode via Tailwind `dark:` — every new component must look right in both.
- Typography: Inter or Geist Sans for UI. **All currency, counts, and rates render with a mono face + `tabular-nums`** so columns of numbers align vertically:

```tsx
<span className="font-mono tabular-nums">{formatCurrency(amount)}</span>
```

## App shell

- **Desktop**: left collapsible sidebar — top: Mess Switcher dropdown (active mess name + role badge + "+ Create / Join Mess"); middle: Overview, Meal Sheet, Expenses & Deposits, Members, Financial Reports; bottom: Mess Settings (managers only) + profile menu.
- **Top utility bar**: Active Month Switcher (`November 2026`) that re-scopes all dashboard queries, `+ Quick Entry` dropdown (managers), notification popover.
- **Mobile**: sidebar collapses to a fixed **bottom nav** with 4 tabs (Overview, Meals, Expenses, Summary) + slide-out drawer for mess switching. Mobile-first: build the narrow layout first, then enhance.

## Shadcn UI usage

- Primitives live in `src/components/ui/` (Button, Dialog, DropdownMenu, Table, Sheet…). Add via Shadcn CLI; do not hand-roll what Radix provides (dialogs, popovers, dropdowns, comboboxes).
- Slide-over **Sheet** is the standard container for create/edit flows (Add Bazar, Invite Member, utility payment entry).
- Feedback components in `src/components/feedback/`: toasts for mutation results, skeletons for every loading table/card, error boundaries per route segment.

## Component placement

- Shared primitives → `src/components/`; domain-specific components → `src/features/<domain>/components/` (per `frontend/AGENTS.md` §2). A component referencing a domain's types belongs to that feature, never to shared.
