/** Display formatters — money/counts always render with `.num` in the JSX (mono + tabular-nums). */

export function formatMoney(value: number): string {
  return new Intl.NumberFormat("en-IN", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value);
}

/** Meal counts are 0.5-step decimals: show 2 not 2.0, and 0.5 not ½. */
export function formatCount(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 }).format(value);
}

/** ISO YYYY-MM-DD → locale date; tolerates bad input (renders as-is). */
export function formatDay(iso: string): string {
  const date = new Date(`${iso}T00:00:00`);
  return Number.isNaN(date.getTime()) ? iso : date.toLocaleDateString();
}

/** Month helpers — the UI store's activeMonth {year, month(1-based)} scopes monthly queries. */
export function monthRange(year: number, month: number): { from: string; to: string } {
  const pad = (n: number) => String(n).padStart(2, "0");
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return { from: `${year}-${pad(month)}-01`, to: `${year}-${pad(month)}-${pad(lastDay)}` };
}

export function todayIso(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
