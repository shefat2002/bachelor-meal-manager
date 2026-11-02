"use client";

import { Check, Copy, ShieldCheck, UserRoundX } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  useApproveMember,
  useCreateInvite,
  useMembers,
  useRejectMember,
  useRemoveMember,
  useUpdateMemberRole,
} from "@/features/messes/hooks";
import { useAuth } from "@/lib/auth-context";

/** Member directory: pending-approvals banner (managers), invite generator, roles, leave (§3.5). */
export default function MembersPage() {
  const { messId } = useCurrentMess();
  const { user } = useAuth();
  const { data, isLoading } = useMembers(messId);
  const createInvite = useCreateInvite(messId);
  const approve = useApproveMember(messId);
  const reject = useRejectMember(messId);
  const updateRole = useUpdateMemberRole(messId);
  const remove = useRemoveMember(messId);
  const [inviteToken, setInviteToken] = useState<string | null>(null);

  const members = data?.items ?? [];
  const pending = members.filter((m) => m.status === "Pending");
  const active = members.filter((m) => m.status === "Active");

  const onInvite = async () => {
    try {
      const result = await createInvite.mutateAsync();
      setInviteToken(result.inviteToken);
    } catch {
      toast.error("Could not create an invite.");
    }
  };

  const inviteUrl = inviteToken
    ? `${typeof window !== "undefined" ? window.location.origin : ""}/invite/${inviteToken}`
    : null;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">Members</h1>
        <Authorize roles={["MealManager", "superAdmin"]}>
          <Button onClick={onInvite} disabled={createInvite.isPending}>
            Invite member
          </Button>
        </Authorize>
      </div>

      {inviteUrl && (
        <Card>
          <CardContent className="flex flex-wrap items-center gap-2 py-4 text-sm">
            <span className="text-muted-foreground">Invite link (valid 7 days):</span>
            <code className="max-w-full truncate rounded bg-muted px-2 py-1 text-xs">
              {inviteUrl}
            </code>
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                void navigator.clipboard.writeText(inviteUrl);
                toast.success("Invite link copied");
              }}
            >
              <Copy className="size-4" /> Copy
            </Button>
          </CardContent>
        </Card>
      )}

      {pending.length > 0 && (
        <Authorize roles={["MealManager", "superAdmin"]}>
          <Card className="border-pending/40 bg-pending-muted">
            <CardContent className="space-y-3 py-4">
              <p className="text-sm font-medium text-pending">
                {pending.length} join request{pending.length === 1 ? "" : "s"} awaiting approval
              </p>
              {pending.map((m) => (
                <div key={m.memberId} className="flex items-center justify-between gap-2 text-sm">
                  <span>
                    {m.name} · <span className="num text-muted-foreground">{m.phone}</span>
                  </span>
                  <span className="flex gap-2">
                    <Button size="sm" variant="outline" onClick={() => reject.mutate(m.memberId)}>
                      <UserRoundX className="size-4" /> Reject
                    </Button>
                    <Button size="sm" onClick={() => approve.mutate(m.memberId)}>
                      <Check className="size-4" /> Approve
                    </Button>
                  </span>
                </div>
              ))}
            </CardContent>
          </Card>
        </Authorize>
      )}

      {isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
        </div>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Joined</TableHead>
                <TableHead className="w-10" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {active.map((m) => (
                <TableRow key={m.memberId}>
                  <TableCell className="font-medium">{m.name}</TableCell>
                  <TableCell className="num">{m.phone}</TableCell>
                  <TableCell>
                    <Badge variant={m.role === "MealManager" ? "default" : "secondary"}>
                      {m.role === "MealManager" ? "Manager" : "Member"}
                    </Badge>
                  </TableCell>
                  <TableCell className="num text-muted-foreground">
                    {new Date(m.joinedAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <Authorize roles={["MealManager", "superAdmin"]}>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon" aria-label={`Actions for ${m.name}`}>
                            ⋯
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          {m.role === "Member" ? (
                            <DropdownMenuItem
                              onClick={() =>
                                updateRole.mutate({ memberId: m.memberId, role: "MealManager" })
                              }
                            >
                              <ShieldCheck className="size-4" /> Promote to Manager
                            </DropdownMenuItem>
                          ) : (
                            <DropdownMenuItem
                              onClick={() =>
                                updateRole.mutate({ memberId: m.memberId, role: "Member" })
                              }
                            >
                              Demote to Member
                            </DropdownMenuItem>
                          )}
                          {m.memberId !== user?.id && (
                            <DropdownMenuItem
                              variant="destructive"
                              onClick={() =>
                                remove.mutate(m.memberId, {
                                  onError: (error) =>
                                    toast.error(
                                      (error as { response?: { data?: { detail?: string } } })
                                        ?.response?.data?.detail ?? "Could not remove member.",
                                    ),
                                })
                              }
                            >
                              Remove from mess
                            </DropdownMenuItem>
                          )}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </Authorize>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <p className="text-sm text-muted-foreground">
        Want to leave this mess?{" "}
        <Link className="text-primary hover:underline" href="/dashboard">
          Manage your messes
        </Link>
        .
      </p>
    </div>
  );
}
