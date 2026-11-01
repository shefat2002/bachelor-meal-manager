/**
 * Session/token store (skeleton — full wiring lands in P1-FE-2).
 *
 * Access token lives in memory only (never localStorage — XSS). The refresh
 * token is a secure HTTP-only cookie managed by the backend, so the client
 * never touches it; refresh is a plain POST without a body token.
 */

let accessToken: string | null = null;
let sessionExpiredHandler: (() => void) | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string): void {
  accessToken = token;
}

export function clearSession(): void {
  accessToken = null;
}

/** AuthProvider installs the redirect-to-login handler (Phase 1). */
export function onSessionExpired(handler: () => void): void {
  sessionExpiredHandler = handler;
}

export function notifySessionExpired(): void {
  sessionExpiredHandler?.();
}

/**
 * Single-flight refresh (P1-FE-2 wires the real `POST /api/auth/refresh` call
 * and updates the stored access token). Until then a 401 ends the session.
 */
export async function refreshAccessToken(): Promise<string | null> {
  return null;
}
