---
name: bmm-forms-validation
description: Form handling and validation conventions for Bachelor Meal Manager using React Hook Form + Zod + useFieldArray — multi-row bazar entry forms with auto-summed subtotals, daily meal sheets, and Zod schemas mirroring backend FluentValidation rules 1:1. Use when building or editing any form, field array, or input validation in the frontend.
---

# Dynamic Forms & Schema Validation (React Hook Form + Zod)

Specs: `frontend/AGENTS.md` §5.2, `docs/SKILL_BUILDER.md` (frontend §4). All forms use **React Hook Form + Zod** — no form state in component state, no manual validation logic.

## Golden rule: mirror the backend 1:1

Every Zod schema corresponds to a backend FluentValidation validator and encodes the **same constraints** (required, lengths, numeric ranges, date rules). Before writing a schema, find the backend validator (or `docs/BACKEND_PROJECT_PLAN.md` §6 for the endpoint contract) and copy its rules exactly — client and server error boundaries must match so users never see server rejections for something the form should have caught.

Keep schemas next to their feature (`src/features/<domain>/types/` or `schemas.ts`), typed with `z.infer<typeof schema>` as the form and payload type.

## Field arrays (batch bazar entry, meal sheets)

Multi-row entry uses `useFieldArray`:

```tsx
const schema = z.object({
  date: z.string().min(1),
  items: z.array(z.object({
    name: z.string().min(1, 'Item name required'),
    quantity: z.coerce.number().positive(),
    price: z.coerce.number().positive('Price must be > 0'),
  })).min(1, 'Add at least one item'),
});
type BazarForm = z.infer<typeof schema>;

const { register, control, watch, handleSubmit } = useForm<BazarForm>({ resolver: zodResolver(schema) });
const { fields, append, remove } = useFieldArray({ control, name: 'items' });
```

- `+ Add row` appends a blank row; row trash icon removes (keep ≥ 1).
- **Auto-calculated subtotal** shown live via `watch('items')` — `Σ qty × price` rendered with `tabular-nums` mono styling. Never make the user compute or type the total.

## UX requirements

- Real-time validation feedback: errors under fields on blur/submit, inline, in the user's language.
- Submit button disabled while `isSubmitting`; server-side field errors mapped back onto the form via `setError`.
- Forms live in Shadcn `Sheet` (drawer) for quick entry flows (see `bmm-design-system-app-shell`).
- On success: close drawer, toast, and let the mutation's `onSettled` invalidate caches (see `bmm-server-state`).

## Gotchas

- Use `z.coerce.number()` for numeric inputs (HTML inputs yield strings) but beware empty strings → `NaN`; use `.or(z.nan())`-safe patterns or keep raw string fields and transform at submit.
- Meal count steppers on the daily sheet are **mutations, not forms** — they use optimistic updates (skill `bmm-server-state`), not RHF.
