"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Check, HandCoins, Plus, X } from "lucide-react";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
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
  useApproveDeposit,
  useCreateDeposit,
  useDeposits,
  useRejectDeposit,
} from "@/features/expenses/hooks";
import { useMembers } from "@/features/messes/hooks";
import { depositSchema, type DepositInput } from "@/features/expenses/schemas";
import { formatDay, formatMoney, monthRange, todayIso } from "@/lib/format";
import { useUiStore } from "@/lib/stores/ui";
import type { DepositStatus } from "@/types/api";

const ALL = "__all__";

const statusFilterValues: DepositStatus[] = ["Pending", "Approved", "Rejected"];

/** Deposits tab: month-scoped ledger + member self-deposit sheet + manager approval toggles. */
export function DepositsTab() {
  const { messId } = useCurrentMess();
  const activeMonth = useUiStore((s) => s.activeMonth);
  const [statusFilter, setStatusFilter] = useState<string>(ALL);
  const [sheetOpen, setSheetOpen] = useState(false);

  const { from, to } = monthRange(activeMonth.year, activeMonth.month);
  const params = useMemo(
    () => ({
      from,
      to,
      page: 1,
      pageSize: 100,
      sortBy: "date",
      sortDir: "desc",
      ...(statusFilter !== ALL ? { status: statusFilter } : {}),
    }),
    [from, to, statusFilter],
  );

  const deposits = useDeposits(messId, params);
  const approve = useApproveDeposit(messId);
  const reject = useRejectDeposit(messId);

  const rows = deposits.data?.items ?? [];
  const approvedTotal = rows
    .filter((d) => d.status === "Approved")
    .reduce((s, d) => s + d.amount, 0);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-40" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>All statuses</SelectItem>
            {statusFilterValues.map((s) => (
              <SelectItem key={s} value={s}>
                {s}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button onClick={() => setSheetOpen(true)}>
          <Plus className="size-4" /> Add deposit
        </Button>
      </div>

      {deposits.isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
        </div>
      ) : rows.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-10 text-center text-sm text-muted-foreground">
            <HandCoins className="size-6" />
            No deposits this month yet.
          </CardContent>
        </Card>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Member</TableHead>
                <TableHead>Date</TableHead>
                <TableHead>Note</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-10" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((d) => (
                <TableRow key={d.id}>
                  <TableCell className="font-medium">{d.memberName}</TableCell>
                  <TableCell className="num text-muted-foreground">{formatDay(d.date)}</TableCell>
                  <TableCell className="max-w-40 truncate text-muted-foreground">
                    {d.note ?? "—"}
                  </TableCell>
                  <TableCell className="num text-right font-medium">
                    {formatMoney(d.amount)}
                  </TableCell>
                  <TableCell>
                    {d.status === "Approved" ? (
                      <span className="badge-surplus">Approved</span>
                    ) : d.status === "Pending" ? (
                      <span className="badge-pending">Pending</span>
                    ) : (
                      <span className="badge-arrear">Rejected</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <Authorize roles={["MealManager", "superAdmin"]}>
                      {d.status === "Pending" && (
                        <span className="flex gap-1">
                          <Button
                            size="icon-xs"
                            aria-label={`Approve deposit from ${d.memberName}`}
                            disabled={approve.isPending}
                            onClick={() => approve.mutate(d.id)}
                          >
                            <Check className="size-3.5" />
                          </Button>
                          <Button
                            size="icon-xs"
                            variant="outline"
                            aria-label={`Reject deposit from ${d.memberName}`}
                            disabled={reject.isPending}
                            onClick={() => reject.mutate(d.id)}
                          >
                            <X className="size-3.5" />
                          </Button>
                        </span>
                      )}
                    </Authorize>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {approvedTotal > 0 && (
        <p className="text-sm text-muted-foreground">
          Approved this month:{" "}
          <span className="num font-medium text-foreground">{formatMoney(approvedTotal)}</span>
        </p>
      )}

      <AddDepositSheet open={sheetOpen} onOpenChange={setSheetOpen} />
    </div>
  );
}

function AddDepositSheet({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { messId, myRole } = useCurrentMess();
  const members = useMembers(messId);
  const create = useCreateDeposit(messId);
  const isManager = myRole === "MealManager" || myRole === "superAdmin";
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<z.input<typeof depositSchema>, unknown, DepositInput>({
    resolver: zodResolver(depositSchema),
    defaultValues: { amount: undefined, date: todayIso() },
  });

  const onSubmit = handleSubmit(async (input) => {
    await create.mutateAsync(
      input.memberId === "__self" ? { ...input, memberId: undefined } : input,
    );
    toast.success(
      isManager
        ? "Deposit recorded (auto-approved)"
        : "Deposit recorded — pending manager approval",
    );
    onOpenChange(false);
    reset({ amount: undefined, date: todayIso() });
  });

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-sm">
        <SheetHeader>
          <SheetTitle>Add deposit</SheetTitle>
          <SheetDescription>
            {isManager
              ? "Recorded as the manager — auto-approved."
              : "Your own contribution — a manager approves it."}
          </SheetDescription>
        </SheetHeader>
        <form className="flex flex-col gap-4 px-4" onSubmit={onSubmit}>
          {isManager && (
            <div className="space-y-2">
              <Label htmlFor="deposit-member">Member</Label>
              <Select
                onValueChange={(v) => setValue("memberId", v === "__self" ? undefined : v)}
                defaultValue="__self"
              >
                <SelectTrigger id="deposit-member" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="__self">Myself</SelectItem>
                  {(members.data?.items ?? [])
                    .filter((m) => m.status === "Active")
                    .map((m) => (
                      <SelectItem key={m.memberId} value={m.memberId}>
                        {m.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                Picking a member records on their behalf.
              </p>
            </div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="deposit-amount">Amount</Label>
              <Input
                id="deposit-amount"
                type="number"
                step="0.01"
                min="0.01"
                {...register("amount")}
              />
              {errors.amount && <p className="text-sm text-arrear">{errors.amount.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="deposit-date">Date</Label>
              <Input id="deposit-date" type="date" max={todayIso()} {...register("date")} />
              {errors.date && <p className="text-sm text-arrear">{errors.date.message}</p>}
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="deposit-note">Note (optional)</Label>
            <Input id="deposit-note" placeholder="Cash / bKash / Nagad…" {...register("note")} />
          </div>
          <SheetFooter className="mt-2 flex-col gap-2 border-t px-0">
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Saving…" : "Save deposit"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
