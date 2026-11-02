"use client";

import { Check, Copy, LogOut, ShieldCheck, UserRoundX } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { toast } from "sonner";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
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

/** Member directory: pending-approvals banner (managers), invite dialog (link + QR), roles, self-leave (§3.5). */
export default function MembersPage() {
  const { messId } = useCurrentMess();
  const { user } = useAuth();
  const router = useRouter();
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

  const onLeave = () => {
    if (!user || !window.confirm(`Leave this mess? You'll need a new invite to rejoin.`)) return;
    remove.mutate(user.id, {
      onSuccess: () => {
        toast.success("You left the mess.");
        router.push("/dashboard");
      },
      onError: (error) =>
        toast.error(
          (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
            "Could not leave the mess.",
        ),
    });
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

      <Dialog open={inviteUrl !== null} onOpenChange={(open) => !open && setInviteToken(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Invite link</DialogTitle>
            <DialogDescription>
              Single-use, valid for 7 days. Share it or the QR code.
            </DialogDescription>
          </DialogHeader>
          {inviteUrl && (
            <div className="flex flex-col items-center gap-4">
              <div className="rounded-lg border bg-white p-3">
                <QRCodeSVG value={inviteUrl} size={176} />
              </div>
              <code className="w-full rounded bg-muted px-2 py-1.5 text-center text-xs break-all">
                {inviteUrl}
              </code>
              <Button
                variant="outline"
                className="w-full"
                onClick={() => {
                  void navigator.clipboard.writeText(inviteUrl);
                  toast.success("Invite link copied");
                }}
              >
                <Copy className="size-4" /> Copy link
              </Button>
            </div>
          )}
        </DialogContent>
      </Dialog>

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

      <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
        <p>
          Manage your messes on the{" "}
          <Link className="text-primary hover:underline" href="/dashboard">
            dashboard
          </Link>
          .
        </p>
        <Button
          variant="outline"
          size="sm"
          className="text-arrear"
          disabled={remove.isPending}
          onClick={onLeave}
        >
          <LogOut className="size-4" /> Leave this mess
        </Button>
      </div>
    </div>
  );
}
