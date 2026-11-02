"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useJoinMess } from "@/features/messes/hooks";

/** Invite link target: /invite/<token> — protected by middleware; joins the mess (pending approval). */
export default function InvitePage() {
  return (
    <Suspense>
      <InviteGate />
    </Suspense>
  );
}

// useParams is a dynamic API under cacheComponents — read inside Suspense.
function InviteGate() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const joinMess = useJoinMess();
  const [result, setResult] = useState<{ messName: string } | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    joinMess.mutate(params.token, {
      onSuccess: (data) => setResult({ messName: data.messName }),
      onError: (error) => {
        setFailed(true);
        const detail = (error as { response?: { data?: { detail?: string } } })?.response?.data
          ?.detail;
        toast.error(detail ?? "This invite is invalid or expired.");
      },
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.token]);

  return (
    <div className="flex min-h-dvh items-center justify-center bg-slate-50 p-4 dark:bg-slate-950">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-xl">
            {result ? "Request sent!" : failed ? "Invite problem" : "Joining mess…"}
          </CardTitle>
          <CardDescription>
            {result
              ? `Your join request for ${result.messName} was sent. A manager must approve it.`
              : failed
                ? "Ask the mess manager for a fresh invite link."
                : "Using the invite token…"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button className="w-full" onClick={() => router.replace("/dashboard")}>
            <Link href="/dashboard">Go to my messes</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
