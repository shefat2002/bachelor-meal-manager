"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { summaryApi } from "@/features/summary/services";

/**
 * Summary query keys per skill bmm-server-state: ['messes', messId, 'summary', { month, year }].
 * The `all` prefix is what every money mutation invalidates (the "money rule", wired in Phase 2).
 */
export const summaryKeys = {
  all: (messId: string) => ["messes", messId, "summary"] as const,
  detail: (messId: string, year: number, month: number) =>
    ["messes", messId, "summary", { month, year }] as const,
};

export function useMessSummary(messId: string, year: number, month: number) {
  return useQuery({
    queryKey: summaryKeys.detail(messId, year, month),
    queryFn: () => summaryApi.get(messId, year, month),
  });
}

export function useSetMonthLock(messId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ year, month, locked }: { year: number; month: number; locked: boolean }) =>
      summaryApi.setMonthLock(messId, year, month, locked).then(() => undefined),
    onSuccess: (_data, variables) => {
      toast.success(variables.locked ? "Month locked — edits are frozen" : "Month unlocked");
      queryClient.invalidateQueries({ queryKey: summaryKeys.all(messId) });
    },
    onError: (error) =>
      toast.error(
        (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
          "Could not change the month lock.",
      ),
  });
}
