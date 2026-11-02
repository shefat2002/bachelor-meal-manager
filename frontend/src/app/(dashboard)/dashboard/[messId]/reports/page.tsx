"use client";

import { Lock, LockOpen, Printer } from "lucide-react";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useMessSummary, useSetMonthLock } from "@/features/summary/hooks";
import { formatCount, formatMoney } from "@/lib/format";
import { useUiStore } from "@/lib/stores/ui";

/**
 * Master settlement table + export/close bar (§3.4). Server-computed rows only — the §5 equations
 * live in Orleans, never derived on the client. Locking freezes the month (backend 409s all writes).
 */
export default function ReportsPage() {
  const { messId } = useCurrentMess();
  const activeMonth = useUiStore((s) => s.activeMonth);
  const summary = useMessSummary(messId, activeMonth.year, activeMonth.month);
  const setLock = useSetMonthLock(messId);

  const monthLabel = new Date(Date.UTC(activeMonth.year, activeMonth.month - 1)).toLocaleDateString(
    undefined,
    { month: "long", year: "numeric", timeZone: "UTC" },
  );

  if (summary.isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-10" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  const data = summary.data;
  const rows = data?.members ?? [];
  const totals = rows.reduce(
    (acc, row) => ({
      units: acc.units + row.mealUnits,
      mealCost: acc.mealCost + row.mealCost,
      utilityShare: acc.utilityShare + row.utilityShare,
      totalCost: acc.totalCost + row.totalCost,
      deposited: acc.deposited + row.deposited,
      balance: acc.balance + row.balance,
    }),
    { units: 0, mealCost: 0, utilityShare: 0, totalCost: 0, deposited: 0, balance: 0 },
  );

  const onToggleLock = () => {
    const locked = data?.locked ?? false;
    const confirmed = window.confirm(
      locked
        ? `Unlock ${monthLabel}? Edits become possible again and the summary recomputes live.`
        : `Lock ${monthLabel}? All meals, expenses and deposits dated in this month become read-only (409).`,
    );
    if (!confirmed) return;
    setLock.mutate({ year: activeMonth.year, month: activeMonth.month, locked: !locked });
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">Settlement</h1>
          <p className="text-sm text-muted-foreground">
            {monthLabel}
            {data?.locked && <span className="badge-pending ml-2 align-middle">Locked</span>}
          </p>
        </div>
        <div className="flex gap-2 print:hidden">
          <Button variant="outline" onClick={() => window.print()}>
            <Printer className="size-4" /> Export PDF
          </Button>
          <Authorize roles={["MealManager", "superAdmin"]}>
            <Button
              variant={data?.locked ? "outline" : "default"}
              disabled={setLock.isPending}
              onClick={onToggleLock}
            >
              {data?.locked ? <LockOpen className="size-4" /> : <Lock className="size-4" />}
              {data?.locked ? "Unlock month" : "Lock month"}
            </Button>
          </Authorize>
        </div>
      </div>

      {rows.length === 0 ? (
        <Card>
          <CardContent className="py-10 text-center text-sm text-muted-foreground">
            No members to settle yet.
          </CardContent>
        </Card>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Member</TableHead>
                <TableHead className="text-right">Meals</TableHead>
                <TableHead className="text-right">Meal cost</TableHead>
                <TableHead className="text-right">Utility share</TableHead>
                <TableHead className="text-right">Total cost</TableHead>
                <TableHead className="text-right">Deposited</TableHead>
                <TableHead className="text-right">Balance</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.memberId}>
                  <TableCell className="font-medium">{row.name}</TableCell>
                  <TableCell className="num text-right">{formatCount(row.mealUnits)}</TableCell>
                  <TableCell className="num text-right">{formatMoney(row.mealCost)}</TableCell>
                  <TableCell className="num text-right">{formatMoney(row.utilityShare)}</TableCell>
                  <TableCell className="num text-right font-medium">
                    {formatMoney(row.totalCost)}
                  </TableCell>
                  <TableCell className="num text-right">{formatMoney(row.deposited)}</TableCell>
                  <TableCell className="text-right">
                    <span
                      className={
                        row.balance > 0
                          ? "badge-surplus"
                          : row.balance < 0
                            ? "badge-arrear"
                            : "badge-pending"
                      }
                    >
                      {formatMoney(row.balance)}
                    </span>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TableCell className="font-medium">Mess total</TableCell>
                <TableCell className="num text-right">{formatCount(totals.units)}</TableCell>
                <TableCell className="num text-right">{formatMoney(totals.mealCost)}</TableCell>
                <TableCell className="num text-right">{formatMoney(totals.utilityShare)}</TableCell>
                <TableCell className="num text-right font-medium">
                  {formatMoney(totals.totalCost)}
                </TableCell>
                <TableCell className="num text-right">{formatMoney(totals.deposited)}</TableCell>
                <TableCell className="num text-right font-semibold">
                  {formatMoney(totals.balance)}
                </TableCell>
              </TableRow>
            </TableFooter>
          </Table>
        </div>
      )}

      <p className="text-xs text-muted-foreground print:hidden">
        Export PDF prints this table via the browser — server-generated PDF reports arrive in Phase
        4.
      </p>
    </div>
  );
}
