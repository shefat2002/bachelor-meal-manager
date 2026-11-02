"use client";

import { useMemo } from "react";

import { useCurrentMess } from "@/components/mess-provider";
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
import { useMembers } from "@/features/messes/hooks";
import { useMonthlyMeals } from "@/features/meals/hooks";
import { useUiStore } from "@/lib/stores/ui";
import { formatCount } from "@/lib/format";

/**
 * Monthly matrix (desktop spreadsheet view): sticky member column, day columns 1..N,
 * sticky monthly-total column. Read-only — edits happen in the daily view. The top-bar
 * month switcher (Zustand activeMonth) scopes the grid.
 */
export function MonthlyMealMatrix() {
  const { messId } = useCurrentMess();
  const activeMonth = useUiStore((s) => s.activeMonth);
  const members = useMembers(messId);
  const meals = useMonthlyMeals(messId, activeMonth.year, activeMonth.month);

  const daysInMonth = useMemo(
    () => new Date(Date.UTC(activeMonth.year, activeMonth.month, 0)).getUTCDate(),
    [activeMonth],
  );

  const activeMembers = useMemo(
    () => (members.data?.items ?? []).filter((m) => m.status === "Active"),
    [members.data],
  );

  // member → day → total units
  const grid = useMemo(() => {
    const map = new Map<string, Map<number, number>>();
    for (const row of meals.data ?? []) {
      const day = Number(row.date.slice(8, 10));
      const byDay = map.get(row.memberId) ?? new Map<number, number>();
      byDay.set(day, (byDay.get(day) ?? 0) + row.totalUnits);
      map.set(row.memberId, byDay);
    }
    return map;
  }, [meals.data]);

  const monthTotals = useMemo(() => {
    const totals = new Map<string, number>();
    let grand = 0;
    for (const member of activeMembers) {
      let sum = 0;
      for (const units of grid.get(member.memberId)?.values() ?? []) sum += units;
      totals.set(member.memberId, sum);
      grand += sum;
    }
    return { totals, grand };
  }, [activeMembers, grid]);

  if (members.isLoading || meals.isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-10" />
        <Skeleton className="h-14" />
        <Skeleton className="h-14" />
        <Skeleton className="h-14" />
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="sticky left-0 z-20 bg-background text-nowrap">Member</TableHead>
            {Array.from({ length: daysInMonth }, (_, i) => (
              <TableHead key={i + 1} className="num w-12 text-center text-xs">
                {i + 1}
              </TableHead>
            ))}
            <TableHead className="sticky right-0 z-20 bg-background text-center">Total</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {activeMembers.map((member) => (
            <TableRow key={member.memberId}>
              <TableCell className="sticky left-0 z-10 bg-background font-medium text-nowrap">
                {member.name}
              </TableCell>
              {Array.from({ length: daysInMonth }, (_, i) => {
                const units = grid.get(member.memberId)?.get(i + 1);
                return (
                  <TableCell key={i + 1} className="num text-center text-sm text-muted-foreground">
                    {units ? formatCount(units) : "·"}
                  </TableCell>
                );
              })}
              <TableCell className="num sticky right-0 z-10 bg-background text-center font-semibold">
                {formatCount(monthTotals.totals.get(member.memberId) ?? 0)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
        <TableFooter>
          <TableRow>
            <TableCell className="sticky left-0 z-10 bg-background font-medium">
              Month total
            </TableCell>
            {Array.from({ length: daysInMonth }, (_, i) => {
              let dayTotal = 0;
              for (const member of activeMembers)
                dayTotal += grid.get(member.memberId)?.get(i + 1) ?? 0;
              return (
                <TableCell key={i + 1} className="num text-center text-xs">
                  {dayTotal ? formatCount(dayTotal) : "·"}
                </TableCell>
              );
            })}
            <TableCell className="num sticky right-0 z-10 bg-background text-center font-semibold">
              {formatCount(monthTotals.grand)}
            </TableCell>
          </TableRow>
        </TableFooter>
      </Table>
    </div>
  );
}
