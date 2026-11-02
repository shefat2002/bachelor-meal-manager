"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import {
  bazarApi,
  depositsApi,
  utilityPaymentsApi,
  utilityTypesApi,
} from "@/features/expenses/services";

/**
 * Query keys per skill bmm-server-state — key arrays mirror the API query params exactly.
 * Money mutations also invalidate ['messes', messId, 'summary'] (Phase 3 cache).
 */
export const expenseKeys = {
  bazar: (messId: string, params: Record<string, unknown>) =>
    ["messes", messId, "expenses", "bazar", params] as const,
  utilityTypes: (messId: string) => ["messes", messId, "utility-types"] as const,
  utility: (messId: string, params: Record<string, unknown>) =>
    ["messes", messId, "expenses", "utility", params] as const,
  deposits: (messId: string, params: Record<string, unknown>) =>
    ["messes", messId, "deposits", params] as const,
  summary: (messId: string) => ["messes", messId, "summary"] as const,
};

const detailToast = (fallback: string) => (error: unknown) =>
  toast.error(
    (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ?? fallback,
  );

function useInvalidateExpenses(messId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ["messes", messId, "expenses"] });
    queryClient.invalidateQueries({ queryKey: ["messes", messId, "deposits"] });
    queryClient.invalidateQueries({ queryKey: ["messes", messId, "utility-types"] });
    queryClient.invalidateQueries({ queryKey: expenseKeys.summary(messId) });
  };
}

// ---- bazar ----

export function useBazarExpenses(messId: string, params: Record<string, unknown>) {
  return useQuery({
    queryKey: expenseKeys.bazar(messId, params),
    queryFn: () => bazarApi.list(messId, params),
    placeholderData: keepPreviousData,
  });
}

export function useCreateBazar(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (payload: Parameters<typeof bazarApi.create>[1]) =>
      bazarApi.create(messId, payload),
    onSuccess: invalidate,
    onError: detailToast("Could not record the bazar expenses."),
  });
}

// ---- utility types ----

export function useUtilityTypes(messId: string) {
  return useQuery({
    queryKey: expenseKeys.utilityTypes(messId),
    queryFn: () => utilityTypesApi.list(messId),
  });
}

export function useCreateUtilityType(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (payload: { name: string; description?: string }) =>
      utilityTypesApi.create(messId, payload),
    onSuccess: invalidate,
    onError: detailToast("Could not create the utility type."),
  });
}

export function useUpdateUtilityType(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: ({
      id,
      ...payload
    }: {
      id: string;
      name: string;
      description?: string;
      isActive: boolean;
    }) => utilityTypesApi.update(messId, id, payload),
    onSuccess: invalidate,
    onError: detailToast("Could not update the utility type."),
  });
}

// ---- utility payments ----

export function useUtilityPayments(messId: string, params: Record<string, unknown>) {
  return useQuery({
    queryKey: expenseKeys.utility(messId, params),
    queryFn: () => utilityPaymentsApi.list(messId, params),
  });
}

export function useCreateUtilityPayment(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (payload: Parameters<typeof utilityPaymentsApi.create>[1]) =>
      utilityPaymentsApi.create(messId, payload),
    onSuccess: invalidate,
    onError: detailToast("Could not record the utility payment."),
  });
}

// ---- deposits ----

export function useDeposits(messId: string, params: Record<string, unknown>) {
  return useQuery({
    queryKey: expenseKeys.deposits(messId, params),
    queryFn: () => depositsApi.list(messId, params),
    placeholderData: keepPreviousData,
  });
}

export function useCreateDeposit(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (payload: { memberId?: string; amount: number; date: string; note?: string }) =>
      depositsApi.create(messId, payload),
    onSuccess: invalidate,
    onError: detailToast("Could not record the deposit."),
  });
}

export function useApproveDeposit(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (id: string) => depositsApi.approve(messId, id).then(() => undefined),
    onSuccess: invalidate,
    onError: detailToast("Could not approve the deposit."),
  });
}

export function useRejectDeposit(messId: string) {
  const invalidate = useInvalidateExpenses(messId);
  return useMutation({
    mutationFn: (id: string) => depositsApi.reject(messId, id).then(() => undefined),
    onSuccess: invalidate,
    onError: detailToast("Could not reject the deposit."),
  });
}
