# P0-FE-1: Shadcn UI + design tokens + Prettier

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Frontend

## What was built

The bare create-next-app scaffold now has Shadcn UI (Radix primitives, `radix-nova` style) with the project's canonical design tokens wired into `globals.css` (Tailwind v4 CSS variables), eleven base components, and Prettier as the enforced formatter. Lint + build green.

## Files touched

- `components.json` — shadcn config (style `radix-nova`, cssVariables, lucide; aliases `@/components/ui` etc.)
- `src/app/globals.css` — tokens mapped per `docs/FRONTEND_DESIGN_OVERVIEW.md` §1: primary `indigo-600`, light bg white/slate-50, dark bg `slate-950`, ring indigo, chart palette (indigo/emerald/rose/amber/slate), semantic vars `--surplus` (emerald-600 on emerald-50), `--arrear` (rose on rose-50), `--pending` (amber) with dark-mode variants; component classes `badge-surplus/badge-arrear/badge-pending` and `.num` (`font-mono tabular-nums`). Also fixed shadcn's self-referential `--font-sans: var(--font-sans)` → `var(--font-geist-sans)`.
- `src/components/ui/` — button, badge, card, input, label, table, dialog, dropdown-menu, sheet, skeleton, sonner (11 files via CLI)
- `.prettierrc` / `.prettierignore`; `format`/`format:check` scripts; devDeps prettier + prettier-plugin-tailwindcss (printWidth 100, tailwind stylesheet aware)

## Key decisions & why

- **New shadcn CLI majors**: `-b radix -p nova` (Radix + Lucide/Geist preset) — matches the spec's "Radix Primitives" and existing Geist fonts; base color left neutral with our token overrides on top.
- **Tokens as CSS vars + tiny component classes** — keeps shadcn theming intact while making surplus/arrear/pending first-class (`bg-surplus-muted text-surplus`), both themes.
- **Prettier normalized markdown bullets** in `frontend/AGENTS.md` (`*` → `-`, content untouched) — one-time churn, consistent style going forward.

## How to verify / test

```bash
cd frontend && npm run lint && npm run build && npm run format:check
```

## Follow-ups / known gaps

- `next-themes` (pulled in by sonner) needs a ThemeProvider + `.dark` toggle in P1-FE-3 (app shell).
