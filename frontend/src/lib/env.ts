/**
 * Typed env config. `NEXT_PUBLIC_API_BASE_URL` overrides the default, which
 * matches the Aspire AppHost's fixed api port (backend/src/MealManager.AppHost).
 */
const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export const env = {
  apiBaseUrl,
} as const;
