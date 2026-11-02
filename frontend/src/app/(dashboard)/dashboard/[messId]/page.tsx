"use client";

import { HandCoins, ShoppingCart, Utensils } from "lucide-react";
import { useMemo } from "react";

import { useCurrentMess } from "@/components/mess-provider";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useBazarExpenses, useDeposits } from "@/features/expenses/hooks";
import { useDailyMeals } from "@/features/meals/hooks";
import { TrendChart } from "@/features/summary/components/trend-chart";
import { useMessSummary } from "@/features/summary/hooks";
import { useAuth } from "@/lib/auth-context";
import { formatCount, formatMoney, monthRange, todayIso } from "@/lib/format";
import { useUiStore } from "@/lib/stores/ui";

/** Overview dashboard: 4 §5 KPI cards + spend/volume trend + today's snapshot + recent activity (§3.1). */
export default function MessOverviewPage() {
  const { mess, myRole, messId } = useCurrentMess();
  const { user } = useAuth();
  const activeMonth = useUiStore((s) => s.activeMonth);

  const summary = useMessSummary(messId, activeMonth.year, activeMonth.month);
  const today = useDailyMeals(messId, todayIso());
  const { from, to } = monthRange(activeMonth.year, activeMonth.month);
  const bazar = useBazarExpenses(messId, {
    from,
    to,
    page: 1,
    pageSize: 5,
    sortBy: "date",
    sortDir: "desc",
  });
  const deposits = useDeposits(messId, {
    from,
    to,
    page: 1,
    pageSize: 5,
    sortBy: "date",
    sortDir: "desc",
  });

  const data = summary.data;
  const myRow = useMemo(
    () => data?.members.find((m) => m.memberId === user?.id) ?? null,
    [data, user?.id],
  );

  const eatingToday = useMemo(
    () => (today.data?.items ?? []).filter((row) => row.totalUnits > 0),
    [today.data],
  );
  const unitsToday = eatingToday.reduce((sum, row) => sum + row.totalUnits, 0);
  const iAmEating = eatingToday.some((row) => row.memberId === user?.id);

  const activity = useMemo(() => {
    const bazarItems = (bazar.data?.items ?? []).map((row) => ({
      key: row.id,
      icon: ShoppingCart,
      text: `${row.itemName} bought`,
      amount: row.lineTotal,
      at: row.createdAt,
    }));
    const depositItems = (deposits.data?.items ?? []).map((row) => ({
      key: row.id,
      icon: HandCoins,
      text: `${row.memberName} deposited (${row.status.toLowerCase()})`,
      amount: row.amount,
      at: row.createdAt,
    }));
    return [...bazarItems, ...depositItems].sort((a, b) => b.at.localeCompare(a.at)).slice(0, 6);
  }, [bazar.data, deposits.data]);

  if (summary.isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-56" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-28" />
          ))}
        </div>
        <Skeleton className="h-[320px]" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold">{mess?.name}</h1>
        <p className="text-sm text-muted-foreground">
          Role: {myRole === "MealManager" ? "Meal Manager" : myRole} ·{" "}
          {new Date(Date.UTC(activeMonth.year, activeMonth.month - 1)).toLocaleDateString(
            undefined,
            {
              month: "long",
              year: "numeric",
              timeZone: "UTC",
            },
          )}
          {data?.locked && <span className="badge-pending ml-2 align-middle">Locked</span>}
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label="Meal rate"
          value={formatMoney(data?.mealRate ?? 0)}
          sub={`${formatCount(data?.totalMealUnits ?? 0)} units · ${formatMoney(data?.totalMealCosts ?? 0)} bazar`}
        />
        <KpiCard
          label="On-hand balance"
          value={formatMoney(data?.onHandBalance ?? 0)}
          tone={(data?.onHandBalance ?? 0) >= 0 ? "surplus" : "arrear"}
          sub={`${formatMoney(data?.totalDeposits ?? 0)} collected`}
        />
        <KpiCard
          label="Total expenditure"
          value={formatMoney(data?.totalExpenditure ?? 0)}
          sub={`bazar ${formatMoney(data?.totalMealCosts ?? 0)} + utility ${formatMoney(data?.totalUtilityCosts ?? 0)}`}
        />
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-muted-foreground">My status</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-1">
            {myRow ? (
              <>
                <span
                  className={`num text-2xl font-semibold ${
                    myRow.balance > 0 ? "text-surplus" : myRow.balance < 0 ? "text-arrear" : ""
                  }`}
                >
                  {formatMoney(myRow.balance)}
                </span>
                <span className="text-xs text-muted-foreground">
                  {myRow.balance > 0 ? "Surplus" : myRow.balance < 0 ? "Arrear" : "Settled"} ·{" "}
                  {formatMoney(myRow.totalCost)} cost
                </span>
              </>
            ) : (
              <span className="text-sm text-muted-foreground">—</span>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Bazar spend vs meal volume</CardTitle>
          </CardHeader>
          <CardContent>
            <TrendChart daily={data?.daily ?? []} />
          </CardContent>
        </Card>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Utensils className="size-4" /> Today
              </CardTitle>
            </CardHeader>
            <CardContent className="text-sm">
              <p>
                <span className="num text-2xl font-semibold">{formatCount(unitsToday)}</span>{" "}
                <span className="text-muted-foreground">meals</span>
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                {eatingToday.length > 0
                  ? `${eatingToday.length} member${eatingToday.length === 1 ? "" : "s"} eating${iAmEating ? " (you)" : ""}`
                  : "Nobody has logged meals today"}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Recent activity</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {activity.length === 0 && (
                <p className="text-sm text-muted-foreground">Nothing this month yet.</p>
              )}
              {activity.map((item) => (
                <div key={item.key} className="flex items-center justify-between gap-2 text-sm">
                  <span className="flex min-w-0 items-center gap-2">
                    <item.icon className="size-3.5 shrink-0 text-muted-foreground" />
                    <span className="truncate">{item.text}</span>
                  </span>
                  <span className="num shrink-0 font-medium">{formatMoney(item.amount)}</span>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

function KpiCard({
  label,
  value,
  sub,
  tone,
}: {
  label: string;
  value: string;
  sub?: string;
  tone?: "surplus" | "arrear";
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-1">
        <span
          className={`num text-2xl font-semibold ${
            tone === "surplus" ? "text-surplus" : tone === "arrear" ? "text-arrear" : ""
          }`}
        >
          {value}
        </span>
        {sub && <span className="num text-xs text-muted-foreground">{sub}</span>}
      </CardContent>
    </Card>
  );
}
