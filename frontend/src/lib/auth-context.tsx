"use client";

import { useRouter } from "next/navigation";
import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

import type { AuthUser } from "@/types/api";
import { clearSession, getCachedUser, onSessionExpired, refreshAccessToken } from "@/lib/auth";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

interface AuthState {
  user: AuthUser | null;
  status: AuthStatus;
}

const AuthContext = createContext<AuthState>({ user: null, status: "loading" });

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ user: null, status: "loading" });
  const router = useRouter();

  useEffect(() => {
    // Session restore: silent refresh (HttpOnly cookie) → user via the refreshed token.
    let cancelled = false;
    refreshAccessToken().then((token) => {
      if (cancelled) {
        return;
      }

      const user = getCachedUser();
      setState(
        token && user
          ? { user, status: "authenticated" }
          : { user: null, status: "unauthenticated" },
      );
    });

    onSessionExpired(() => {
      clearSession();
      setState({ user: null, status: "unauthenticated" });
      router.replace(`/login?next=${encodeURIComponent(window.location.pathname)}`);
    });

    return () => {
      cancelled = true;
    };
  }, [router]);

  return <AuthContext.Provider value={state}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  return useContext(AuthContext);
}
