"use client";

import { env } from "@/lib/env";

/**
 * In-memory token store (skill bmm-app-router-multitenancy): access token in memory only,
 * refresh token in the HttpOnly `bmm_refresh` cookie owned by the API. Never localStorage.
 */

let accessToken: string | null = null;
let authUser: import("@/types/api").AuthUser | null = null;
let sessionExpiredHandler: (() => void) | null = null;
let refreshInFlight: Promise<string | null> | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string): void {
  accessToken = token;
}

export function getCachedUser() {
  return authUser;
}

export function setCachedUser(user: import("@/types/api").AuthUser | null): void {
  authUser = user;
}

export function clearSession(): void {
  accessToken = null;
  authUser = null;
}

/** AuthProvider installs the redirect-to-login handler. */
export function onSessionExpired(handler: () => void): void {
  sessionExpiredHandler = handler;
}

export function notifySessionExpired(): void {
  sessionExpiredHandler?.();
}

/** Single-flight refresh against the API's rotating HttpOnly cookie. Returns null when the session is gone. */
export async function refreshAccessToken(): Promise<string | null> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

async function doRefresh(): Promise<string | null> {
  try {
    const response = await fetch(`${env.apiBaseUrl}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
    if (!response.ok) {
      return null;
    }

    const body = (await response.json()) as {
      accessToken: string;
      user: import("@/types/api").AuthUser;
    };
    accessToken = body.accessToken;
    authUser = body.user;
    return body.accessToken;
  } catch {
    return null;
  }
}
