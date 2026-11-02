# P2-FE-3: Expenses hub — bazar tab + Add Bazar drawer

- **Date**: 2026-11-02
- **Agent**: ZCode (Phase 2 frontend session)
- **Phase / track**: Phase 2 / Frontend

## What was built
The bazar tab at `/dashboard/[messId]/expenses`: month-scoped (top-bar switcher) table of bazar lines (Date, Item, Buyer, Qty, Unit price, Line total) with a buyer filter, page-total footer and pagination; the manager-gated `+ Add Bazar` button opens a right-side Sheet with dynamic item rows (`useFieldArray`: name/quantity/unit price), per-row line totals, a **live auto-summed total**, remove-row (min 1), buyer picker (defaults to self), and date. On save it POSTs the batch, toasts, closes and invalidates the bazar + summary caches.

## Files touched
- `frontend/src/features/expenses/services.ts` — `bazarApi` (+ utility/deposit APIs for FE-4).
- `frontend/src/features/expenses/hooks.ts` — `expenseKeys`, `useBazarExpenses` (`placeholderData: keepPreviousData`), `useCreateBazar`, shared `useInvalidateExpenses`.
- `frontend/src/features/expenses/schemas.ts` — zod schemas mirroring backend validators 1:1 (ranges: qty 0.01–99,999; price 0.01–999,999,999; ≤50 items).
- `frontend/src/features/expenses/components/bazar-tab.tsx`, `components/add-bazar-sheet.tsx`.
- `frontend/src/app/(dashboard)/dashboard/[messId]/expenses/page.tsx` — three-tab hub shell.

## Key decisions & why
- **Query params mirrored in the query key** (`{from,to,page,sortBy,sortDir,buyerMemberId?}`) — no two requests share a key (skill bmm-server-state).
- **zod v4 + RHF**: forms use the three-generic `useForm<z.input<S>, unknown, z.output<S>>` — `z.coerce.number()` has `unknown` input, which breaks the single-generic resolver typing. Established as the pattern for all numeric forms.
- Buyer filter + `+ Add Bazar` are the only controls; the table sorts `date desc` server-side (the whitelist's other key, `price`, reserved for a future sort UI).
- Known-benign lint warning: React Compiler skips memoizing the drawer because RHF `watch()` is incompatible with auto-memoization — the forms skill mandates `watch` for auto-sum; accepted.

## How to verify / test
- `npm run lint && npm run build` green; manual: add a 2-item bazar run as manager, verify rows land in the table, member sees table but no `+ Add Bazar`.

## Follow-ups / known gaps
- No bazar edit/delete UI (backend doesn't offer it either) — revisit if managers need corrections.
