"use client";

import { apiClient } from "@/lib/api-client";
import type { MessSummary } from "@/types/api";

export const summaryApi = {
  get: (messId: string, year: number, month: number) =>
    apiClient
      .get<MessSummary>(`/api/summary/messes/${messId}`, {
        params: { year, month },
        headers: { "X-Mess-Id": messId },
      })
      .then((r) => r.data),

  setMonthLock: (messId: string, year: number, month: number, locked: boolean) =>
    apiClient.put(
      `/api/summary/messes/${messId}/month-lock`,
      { year, month, locked },
      { headers: { "X-Mess-Id": messId } },
    ),
};
