"use client";

import { apiClient } from "@/lib/api-client";
import type { DailyMeal, PaginatedResult } from "@/types/api";

export interface MealEntryInput {
  memberId: string;
  breakfast: number;
  lunch: number;
  dinner: number;
}

export interface MealListParams {
  memberId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}

export const mealsApi = {
  list: (messId: string, params: MealListParams) =>
    apiClient
      .get<PaginatedResult<DailyMeal>>("/api/meals", {
        params,
        headers: { "X-Mess-Id": messId },
      })
      .then((r) => r.data),

  /** Bulk upsert for one date (POST /api/meals → 200 with the resulting rows). */
  submit: (messId: string, date: string, entries: MealEntryInput[]) =>
    apiClient
      .post<DailyMeal[]>("/api/meals", { date, entries }, { headers: { "X-Mess-Id": messId } })
      .then((r) => r.data),
};
