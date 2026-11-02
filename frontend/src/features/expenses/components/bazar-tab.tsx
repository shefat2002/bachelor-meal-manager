"use client";

import { ChevronLeft, ChevronRight, Plus, ShoppingCart } from "lucide-react";
import { useMemo, useState } from "react";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
import { AddBazarSheet } from "@/features/expenses/components/add-bazar-sheet";
import { useBazarExpenses } from "@/features/expenses/hooks";
import { useMembers } from "@/features/messes/hooks";
import { formatDay, formatMoney, monthRange } from "@/lib/format";
import { useUiStore } from "@/lib/stores/ui";

const ALL = "__all__";

/** Bazar tab: month-scoped (top-bar switcher) filterable table + manager-gated `+ Add Bazar` drawer. */
export function BazarTab() {
  const { messId } = useCurrentMess();
  const activeMonth = useUiStore((s) => s.activeMonth);
  const members = useMembers(messId);
  const [buyerFilter, setBuyerFilter] = useState<string>(ALL);
  const [page, setPage] = useState(1);
  const [sheetOpen, setSheetOpen] = useState(false);

  const { from, to } = monthRange(activeMonth.year, activeMonth.month);
  const params = useMemo(
    () => ({
      from,
      to,
      page,
      pageSize: 20,
      sortBy: "date",
      sortDir: "desc",
      ...(buyerFilter !== ALL ? { buyerMemberId: buyerFilter } : {}),
    }),
    [from, to, page, buyerFilter],
  );

  const expenses = useBazarExpenses(messId, params);

  const memberName = (id: string) =>
    (members.data?.items ?? []).find((m) => m.memberId === id)?.name ?? "Unknown";

  const activeMembers = (members.data?.items ?? []).filter((m) => m.status === "Active");
  const rows = expenses.data?.items ?? [];
  const pageTotal = rows.reduce((sum, row) => sum + row.lineTotal, 0);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Select
          value={buyerFilter}
          onValueChange={(v) => {
            setBuyerFilter(v);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-44" aria-label="Filter by buyer">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>All buyers</SelectItem>
            {activeMembers.map((m) => (
              <SelectItem key={m.memberId} value={m.memberId}>
                {m.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Authorize roles={["MealManager", "superAdmin"]}>
          <Button onClick={() => setSheetOpen(true)}>
            <Plus className="size-4" /> Add Bazar
          </Button>
        </Authorize>
      </div>

      {expenses.isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
        </div>
      ) : rows.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-10 text-center text-sm text-muted-foreground">
            <ShoppingCart className="size-6" />
            No bazar expenses this month yet.
          </CardContent>
        </Card>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Date</TableHead>
                <TableHead>Item</TableHead>
                <TableHead>Buyer</TableHead>
                <TableHead className="text-right">Qty</TableHead>
                <TableHead className="text-right">Unit price</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id}>
                  <TableCell className="num text-muted-foreground">{formatDay(row.date)}</TableCell>
                  <TableCell className="font-medium">{row.itemName}</TableCell>
                  <TableCell>{memberName(row.buyerMemberId)}</TableCell>
                  <TableCell className="num text-right">{formatMoney(row.quantity)}</TableCell>
                  <TableCell className="num text-right">{formatMoney(row.price)}</TableCell>
                  <TableCell className="num text-right font-medium">
                    {formatMoney(row.lineTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TableCell colSpan={5} className="text-right text-muted-foreground">
                  Page total
                </TableCell>
                <TableCell className="num text-right font-semibold">
                  {formatMoney(pageTotal)}
                </TableCell>
              </TableRow>
            </TableFooter>
          </Table>
        </div>
      )}

      {(expenses.data?.totalCount ?? 0) > (expenses.data?.pageSize ?? 20) && (
        <div className="flex items-center justify-end gap-2 text-sm text-muted-foreground">
          <Button
            variant="outline"
            size="icon-sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            <ChevronLeft className="size-4" />
          </Button>
          <span className="num">
            {page} / {Math.ceil((expenses.data?.totalCount ?? 0) / (expenses.data?.pageSize ?? 20))}
          </span>
          <Button
            variant="outline"
            size="icon-sm"
            disabled={
              page >= Math.ceil((expenses.data?.totalCount ?? 0) / (expenses.data?.pageSize ?? 20))
            }
            onClick={() => setPage((p) => p + 1)}
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>
      )}

      <AddBazarSheet open={sheetOpen} onOpenChange={setSheetOpen} />
    </div>
  );
}
