"use client";

import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { useCreateMess, useJoinMess, useMesses } from "@/features/messes/hooks";

/** Mess selector: list of my messes + create/join actions (P1-FE-5). */
export default function DashboardPage() {
  const { data, isLoading } = useMesses();
  const createMess = useCreateMess();
  const joinMess = useJoinMess();
  const [newName, setNewName] = useState("");
  const [joinToken, setJoinToken] = useState("");

  const onCreate = async () => {
    if (!newName.trim()) {
      return;
    }

    try {
      const mess = await createMess.mutateAsync(newName.trim());
      toast.success(`Mess "${mess.name}" created`);
      setNewName("");
    } catch {
      toast.error("Could not create the mess.");
    }
  };

  const onJoin = async () => {
    if (!joinToken.trim()) {
      return;
    }

    try {
      const result = await joinMess.mutateAsync(joinToken.trim());
      toast.success(`Join request for ${result.messName} sent — waiting for manager approval.`);
      setJoinToken("");
    } catch (error) {
      const detail = (error as { response?: { data?: { detail?: string } } })?.response?.data
        ?.detail;
      toast.error(detail ?? "Could not join with this invite.");
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">My messes</h1>
        <p className="text-sm text-muted-foreground">
          Pick a mess to manage meals, expenses and settlements.
        </p>
      </div>

      {isLoading ? (
        <div className="space-y-3">
          <Skeleton className="h-20" />
          <Skeleton className="h-20" />
        </div>
      ) : data && data.items.length > 0 ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {data.items.map((mess) => (
            <Link key={mess.id} href={`/dashboard/${mess.id}`}>
              <Card className="transition-colors hover:border-primary">
                <CardHeader>
                  <CardTitle className="text-base">{mess.name}</CardTitle>
                  <CardDescription>
                    {mess.activeMemberCount} member{mess.activeMemberCount === 1 ? "" : "s"}
                  </CardDescription>
                </CardHeader>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            You&#39;re not in any mess yet — create one or join with an invite link below.
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Create a mess</CardTitle>
            <CardDescription>You&#39;ll be its Meal Manager.</CardDescription>
          </CardHeader>
          <CardContent className="flex gap-2">
            <Input
              placeholder="Mess name"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && onCreate()}
            />
            <Button onClick={onCreate} disabled={createMess.isPending}>
              Create
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Join a mess</CardTitle>
            <CardDescription>Paste the invite token or open an invite link.</CardDescription>
          </CardHeader>
          <CardContent className="flex gap-2">
            <div className="flex-1 space-y-1">
              <Input
                placeholder="Invite token"
                value={joinToken}
                onChange={(e) => setJoinToken(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && onJoin()}
              />
              <Label className="text-xs text-muted-foreground">
                A manager must approve your join request.
              </Label>
            </div>
            <Button variant="outline" onClick={onJoin} disabled={joinMess.isPending}>
              Join
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
