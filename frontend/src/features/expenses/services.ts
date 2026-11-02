"use client";

import { apiClient } from "@/lib/api-client";
import type {
  BazarExpense,
  Deposit,
  PaginatedResult,
  UtilityPayment,
  UtilityType,
} from "@/types/api";

const tenant = (messId: string) => ({ headers: { "X-Mess-Id": messId } });

export const bazarApi = {
  list: (messId: string, params: Record<string, unknown>) =>
    apiClient
      .get<PaginatedResult<BazarExpense>>("/api/expenses/bazar", { params, ...tenant(messId) })
      .then((r) => r.data),
  create: (
    messId: string,
    payload: {
      date: string;
      buyerMemberId?: string;
      items: { name: string; quantity: number; price: number }[];
    },
  ) =>
    apiClient
      .post<BazarExpense[]>("/api/expenses/bazar", payload, tenant(messId))
      .then((r) => r.data),
};

export const utilityTypesApi = {
  list: (messId: string) =>
    apiClient
      .get<PaginatedResult<UtilityType>>("/api/utility-types", {
        params: { page: 1, pageSize: 100 },
        ...tenant(messId),
      })
      .then((r) => r.data),
  create: (messId: string, payload: { name: string; description?: string }) =>
    apiClient.post<UtilityType>("/api/utility-types", payload, tenant(messId)).then((r) => r.data),
  update: (
    messId: string,
    id: string,
    payload: { name: string; description?: string; isActive: boolean },
  ) =>
    apiClient
      .put<UtilityType>(`/api/utility-types/${id}`, payload, tenant(messId))
      .then((r) => r.data),
  remove: (messId: string, id: string) =>
    apiClient.delete(`/api/utility-types/${id}`, tenant(messId)),
};

export const utilityPaymentsApi = {
  list: (messId: string, params: Record<string, unknown>) =>
    apiClient
      .get<PaginatedResult<UtilityPayment>>("/api/expenses/utility", { params, ...tenant(messId) })
      .then((r) => r.data),
  create: (
    messId: string,
    payload: { utilityTypeId: string; amount: number; paymentDate: string; note?: string },
  ) =>
    apiClient
      .post<UtilityPayment>("/api/expenses/utility", payload, tenant(messId))
      .then((r) => r.data),
};

export const depositsApi = {
  list: (messId: string, params: Record<string, unknown>) =>
    apiClient
      .get<PaginatedResult<Deposit>>("/api/deposits", { params, ...tenant(messId) })
      .then((r) => r.data),
  create: (
    messId: string,
    payload: { memberId?: string; amount: number; date: string; note?: string },
  ) => apiClient.post<Deposit>("/api/deposits", payload, tenant(messId)).then((r) => r.data),
  approve: (messId: string, id: string) =>
    apiClient.put(`/api/deposits/${id}/approve`, null, tenant(messId)),
  reject: (messId: string, id: string) =>
    apiClient.put(`/api/deposits/${id}/reject`, null, tenant(messId)),
};
