"use client";

import type { ReactNode } from "react";

import { useCurrentMess } from "@/components/mess-provider";
import { useAuth } from "@/lib/auth-context";

type Role = "Member" | "MealManager" | "superAdmin";

/**
 * RBAC gate (skill bmm-rbac-route-guards): renders children only when the active
 * mess role (or global role) is allowed. UX only — the backend enforces the same rules.
 */
export function Authorize({
  roles,
  fallback = null,
  children,
}: {
  roles: Role[];
  fallback?: ReactNode;
  children: ReactNode;
}) {
  const { myRole } = useCurrentMess();
  const { user } = useAuth();

  const effective: Role | null = user?.globalRole === "superAdmin" ? "superAdmin" : myRole;
  if (effective && roles.includes(effective)) {
    return <>{children}</>;
  }

  return <>{fallback}</>;
}
