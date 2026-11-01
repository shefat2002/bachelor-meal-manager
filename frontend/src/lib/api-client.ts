import axios, { AxiosError, type AxiosRequestConfig } from "axios";

import { clearSession, getAccessToken, notifySessionExpired, refreshAccessToken } from "@/lib/auth";
import { env } from "@/lib/env";

/**
 * Source of the active mess id, installed by MessProvider (Phase 1). Must be
 * derived from the current route (`[messId]` segment is the only tenant scope),
 * not from state that can go stale — skill bmm-app-router-multitenancy.
 */
let getActiveMessId: () => string | null = () => null;

export function setActiveMessIdSource(getter: () => string | null): void {
  getActiveMessId = getter;
}

export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  const messId = getActiveMessId();
  if (messId) {
    config.headers["X-Mess-Id"] = messId;
  }
  return config;
});

let refreshInFlight: Promise<string | null> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined;
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true;
      // Single-flight: concurrent 401s share one refresh call (guard against refresh loops).
      refreshInFlight ??= refreshAccessToken().finally(() => {
        refreshInFlight = null;
      });
      const token = await refreshInFlight;
      if (token) {
        original.headers ??= {};
        original.headers.Authorization = `Bearer ${token}`;
        return apiClient.request(original);
      }
      clearSession();
      notifySessionExpired();
    }
    return Promise.reject(error);
  },
);
