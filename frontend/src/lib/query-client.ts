import { QueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";

/**
 * TanStack Query defaults — skill bmm-server-state: short staleTime (financial
 * data), retry with backoff except 4xx (client errors won't fix themselves).
 */
export function makeQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: (failureCount, error) => {
          if (isAxiosError(error)) {
            const status = error.response?.status;
            if (status !== undefined && status >= 400 && status < 500) {
              return false;
            }
          }
          return failureCount < 2;
        },
      },
      mutations: {
        retry: false,
      },
    },
  });
}
