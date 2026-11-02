"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { mealsApi, type MealEntryInput } from "@/features/meals/services";
import { monthRange } from "@/lib/format";
import type { DailyMeal, PaginatedResult } from "@/types/api";

/**
 * Query keys per skill bmm-server-state:
 *   ['messes', messId, 'meals', { date }]      — daily sheet
 *   ['messes', messId, 'meals', { month, year }] — monthly matrix
 */
export const mealKeys = {
  all: (messId: string) => ["messes", messId, "meals"] as const,
  day: (messId: string, date: string) => ["messes", messId, "meals", { date }] as const,
  month: (messId: string, year: number, month: number) =>
    ["messes", messId, "meals", { month, year }] as const,
};

/** Daily sheet data: every meal row of one date (from==to covers a mess of ≤100 members). */
export function useDailyMeals(messId: string, date: string) {
  return useQuery({
    queryKey: mealKeys.day(messId, date),
    queryFn: () =>
      mealsApi.list(messId, {
        from: date,
        to: date,
        pageSize: 100,
        sortBy: "date",
        sortDir: "asc",
      }),
    placeholderData: keepPreviousData,
  });
}

/** Monthly matrix data: fetches every page of the month (a month rarely exceeds 100 rows/page… loop to be safe). */
export function useMonthlyMeals(messId: string, year: number, month: number) {
  const { from, to } = monthRange(year, month);
  return useQuery({
    queryKey: mealKeys.month(messId, year, month),
    queryFn: async () => {
      const rows: DailyMeal[] = [];
      let page = 1;
      let totalCount = 0;
      do {
        const result = await mealsApi.list(messId, {
          from,
          to,
          page,
          pageSize: 100,
          sortDir: "asc",
        });
        rows.push(...result.items);
        totalCount = result.totalCount;
        page += 1;
      } while (rows.length < totalCount);
      return rows;
    },
  });
}

/**
 * Optimistic upsert of one date's entries (steppers call this debounced — one mutation per settle).
 * Rollback restores the snapshot; onSettled re-syncs daily + monthly caches and the (Phase 3) summary key.
 */
export function useSubmitDailyMeals(messId: string, date: string) {
  const queryClient = useQueryClient();
  const dayKey = mealKeys.day(messId, date);

  return useMutation({
    mutationFn: (entries: MealEntryInput[]) => mealsApi.submit(messId, date, entries),

    onMutate: async (entries) => {
      await queryClient.cancelQueries({ queryKey: dayKey });
      const previous = queryClient.getQueryData<PaginatedResult<DailyMeal>>(dayKey);
      if (previous) {
        const byMember = new Map(previous.items.map((row) => [row.memberId, { ...row }]));
        for (const entry of entries) {
          const existing = byMember.get(entry.memberId);
          byMember.set(entry.memberId, {
            ...existing,
            id: existing?.id ?? "",
            memberId: entry.memberId,
            date,
            breakfast: entry.breakfast,
            lunch: entry.lunch,
            dinner: entry.dinner,
            totalUnits: entry.breakfast + entry.lunch + entry.dinner,
            createdAt: existing?.createdAt ?? new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          });
        }
        queryClient.setQueryData(dayKey, { ...previous, items: [...byMember.values()] });
      }
      return { previous };
    },

    onError: (error, _entries, context) => {
      queryClient.setQueryData(dayKey, context?.previous);
      toast.error(
        (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
          "Could not save meal counts.",
      );
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: mealKeys.all(messId) });
      // Money rule (skill bmm-server-state): meal writes must refresh the summary cache (Phase 3).
      queryClient.invalidateQueries({ queryKey: ["messes", messId, "summary"] });
    },
  });
}
