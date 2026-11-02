import { Suspense, type ReactNode } from "react";

import { AppShell } from "@/components/app-shell";
import { MessProvider } from "@/components/mess-provider";

/** The tenant boundary: everything below is scoped to [messId] (skill bmm-app-router-multitenancy). */
export default function MessLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ messId: string }>;
}) {
  return (
    <Suspense fallback={<TenantFallback />}>
      <TenantGate params={params}>{children}</TenantGate>
    </Suspense>
  );
}

// params is a dynamic API under cacheComponents — awaited inside Suspense.
async function TenantGate({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ messId: string }>;
}) {
  const { messId } = await params;
  return (
    <MessProvider messId={messId}>
      <AppShell>{children}</AppShell>
    </MessProvider>
  );
}

function TenantFallback() {
  return (
    <div className="flex min-h-dvh items-center justify-center text-sm text-muted-foreground">
      Loading mess…
    </div>
  );
}
