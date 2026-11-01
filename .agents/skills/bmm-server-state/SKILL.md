---
name: bmm-server-state
description: TanStack Query server-state conventions for Bachelor Meal Manager — tenant-scoped hierarchical query keys like ['messes', messId, ...], per-feature data hooks in features/*/hooks, optimistic updates with rollback for meal toggles and steppers, and automatic invalidation of the summary cache whenever meals, bazar expenses, or utility payments change. Use whenever writing data fetching, hooks, mutations, or cache handling in the frontend.
---

# Server State & Optimistic Mutations (TanStack Query)

Specs: `frontend/AGENTS.md` §5.1, `docs/SKILL_BUILDER.md` (frontend §3). Client-side UI state uses Zustand; **everything from the API goes through TanStack Query** — no ad-hoc `useEffect` fetching, no Axios calls inside components.

## Query keys (strict hierarchy, always tenant-scoped)

```
['messes']                                            # mess list for switcher
['messes', messId, 'meals', { date }]                 # daily sheet
['messes', messId, 'meals', { month, year }]          # monthly matrix
['messes', messId, 'expenses', 'bazar', { page, pageSize, filters }]
['messes', messId, 'expenses', 'utility', { month, year }]
['messes', messId, 'members']
['messes', messId, 'summary', { month, year }]
```

- Key arrays mirror API query params exactly — no two different requests share a key.
- The active month selector (top bar) feeds `{ month, year }` into keys; changing month changes the key, which refetches.

## Hooks pattern

Per-feature hooks in `src/features/<domain>/hooks/` wrap `useQuery`/`useMutation`; components never touch `queryClient` or raw keys directly:

```ts
export function useDailyMeals(messId: string, date: string) {
  return useQuery({
    queryKey: ['messes', messId, 'meals', { date }],
    queryFn: () => mealApi.getDaily(messId, date),
  });
}
```

## Optimistic updates (high-frequency interactions)

Meal toggles / stepper increments (`- 0 0.5 1 2 +`) must feel instant — optimistic update with rollback:

```ts
useMutation({
  mutationFn: mealApi.updateDaily,
  onMutate: async (newSheet) => {
    await queryClient.cancelQueries({ queryKey: mealsKey });
    const prev = queryClient.getQueryData(mealsKey);
    queryClient.setQueryData(mealsKey, newSheet);   // instant UI
    return { prev };                                 // context for rollback
  },
  onError: (_e, _v, ctx) => queryClient.setQueryData(mealsKey, ctx?.prev),
  onSettled: () => queryClient.invalidateQueries({ queryKey: mealsKey }),
});
```

Add a subtle pulse animation on changed cells (per design spec §3.2). Debounce stepper spam into one mutation per settle.

## Invalidation rule (the money rule)

Any successful mutation on **meals, bazar expenses, utility payments, or deposits** invalidates `['messes', messId, 'summary']` — meal rate and balances must drift toward correctness in the background automatically. Never let the summary go stale after a write.

## Defaults

Configure once in `lib/query-client.ts`: `staleTime` short (financial data), `retry` with backoff except 4xx, and a global error handler that surfaces toast + auth-redirect on 401.
