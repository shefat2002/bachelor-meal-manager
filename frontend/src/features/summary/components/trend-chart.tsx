"use client";

import { Bar, CartesianGrid, ComposedChart, Line, XAxis, YAxis } from "recharts";

import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { formatMoney } from "@/lib/format";
import type { DailyTrend } from "@/types/api";

const chartConfig = {
  bazarSpend: { label: "Bazar spend", color: "var(--chart-1)" },
  mealUnits: { label: "Meal units", color: "var(--chart-2)" },
} satisfies ChartConfig;

/** Daily bazar spend (bars) vs meal volume (line) for the active month — skill bmm-financial-viz-async-reports. */
export function TrendChart({ daily }: { daily: DailyTrend[] }) {
  if (daily.length === 0) {
    return (
      <div className="flex h-[280px] flex-col items-center justify-center gap-2 text-sm text-muted-foreground">
        <p>No activity this month yet.</p>
        <p className="text-xs">Log meals and bazar expenses to see the trend.</p>
      </div>
    );
  }

  const data = daily.map((point) => ({
    day: Number(point.date.slice(8, 10)),
    bazarSpend: point.bazarSpend,
    mealUnits: point.mealUnits,
  }));

  return (
    <ChartContainer config={chartConfig} className="h-[280px] w-full">
      <ComposedChart data={data} margin={{ left: -12, right: 8 }}>
        <CartesianGrid vertical={false} />
        <XAxis dataKey="day" tickLine={false} axisLine={false} tickMargin={8} className="num" />
        <YAxis yAxisId="left" tickLine={false} axisLine={false} tickMargin={8} className="num" />
        <YAxis
          yAxisId="right"
          orientation="right"
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          className="num"
        />
        <ChartTooltip
          content={
            <ChartTooltipContent
              formatter={(value, name) =>
                name === "Bazar spend" ? formatMoney(Number(value)) : String(value)
              }
              labelFormatter={(label) => `Day ${label}`}
            />
          }
        />
        <Bar yAxisId="left" dataKey="bazarSpend" fill="var(--color-bazarSpend)" radius={4} />
        <Line
          yAxisId="right"
          dataKey="mealUnits"
          stroke="var(--color-mealUnits)"
          strokeWidth={2}
          dot={false}
        />
      </ComposedChart>
    </ChartContainer>
  );
}
