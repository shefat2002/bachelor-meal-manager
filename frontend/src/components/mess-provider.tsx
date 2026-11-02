"use client";

import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { createContext, useContext, useEffect, type ReactNode } from "react";

import { Skeleton } from "@/components/ui/skeleton";
import { useMess } from "@/features/messes/hooks";
import { setActiveMessIdSource } from "@/lib/api-client";
import type { Mess } from "@/types/api";

interface CurrentMess {
  messId: string;
  mess: Mess | null;
  myRole: Mess["myRole"] | null;
  isLoading: boolean;
}

const MessContext = createContext<CurrentMess>({
  messId: "",
  mess: null,
  myRole: null,
  isLoading: true,
});

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * The [messId] segment is the ONLY tenant scope (skill bmm-app-router-multitenancy).
 * Validates the shape, resolves the mess + my role, installs the api-client mess-id
 * source, invalidates the previous mess caches on switch, and renders the "no access"
 * screen when the user isn't a member.
 */
export function MessProvider({ messId, children }: { messId: string; children: ReactNode }) {
  const queryClient = useQueryClient();
  const valid = GUID_RE.test(messId);
  const { data: mess, isLoading, error } = useMess(valid ? messId : "");

  useEffect(() => {
    setActiveMessIdSource(() => (valid ? messId : null));
    return () => setActiveMessIdSource(() => null);
  }, [valid, messId]);

  useEffect(() => {
    // Mess switch: drop stale caches from the previous tenant (skill bmm-server-state).
    return () => {
      queryClient.removeQueries({
        predicate: (query) => query.queryKey[0] === "messes" && query.queryKey[1] === messId,
      });
    };
  }, [messId, queryClient]);

  if (!GUID_RE.test(messId)) {
    return (
      <NoAccess title="Invalid mess link" description="The mess id in the URL is malformed." />
    );
  }

  if (isLoading) {
    return <ShellSkeleton />;
  }

  if (error || !mess) {
    // 404 (and cross-tenant 404s) land here — never leak whether the mess exists.
    return (
      <NoAccess
        title="No access to this mess"
        description="This mess doesn't exist, or you are not a member of it."
      />
    );
  }

  return (
    <MessContext.Provider value={{ messId, mess, myRole: mess.myRole, isLoading }}>
      {children}
    </MessContext.Provider>
  );
}

export function useCurrentMess(): CurrentMess {
  return useContext(MessContext);
}

function ShellSkeleton() {
  return (
    <div className="space-y-4 p-6">
      <Skeleton className="h-9 w-64" />
      <div className="grid gap-4 md:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-28" />
        ))}
      </div>
      <Skeleton className="h-64" />
    </div>
  );
}

function NoAccess({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-2 p-8 text-center">
      <h1 className="text-lg font-semibold">{title}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      <Link className="mt-2 text-sm text-primary hover:underline" href="/dashboard">
        Back to my messes
      </Link>
    </div>
  );
}
