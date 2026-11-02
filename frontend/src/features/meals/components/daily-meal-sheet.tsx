"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { useCurrentMess } from "@/components/mess-provider";
import { Input } from "@/components/ui/input";
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
import { useDailyMeals, useSubmitDailyMeals } from "@/features/meals/hooks";
import { MealStepper } from "@/features/meals/components/meal-stepper";
import { useAuth } from "@/lib/auth-context";
import { formatCount, todayIso } from "@/lib/format";

type Slot = "breakfast" | "lunch" | "dinner";

type Draft = Record<Slot, number>;

/** Debounce window: steppers spam settles into one POST /api/meals per burst (skill bmm-server-state). */
const FLUSH_DEBOUNCE_MS = 700;

/**
 * Daily entry view: one row per active member, `− 0 ½ 1 2 +` steppers per B/L/D slot.
 * Members edit only their own row; managers edit all. Saves are optimistic + debounced.
 */
export function DailyMealSheet() {
  const { messId, myRole } = useCurrentMess();
  const { user } = useAuth();
  const [date, setDate] = useState(todayIso);

  const members = useMembers(messId);
  const meals = useDailyMeals(messId, date);
  const submit = useSubmitDailyMeals(messId, date);

  const isManager = myRole === "MealManager" || myRole === "superAdmin";

  const activeMembers = useMemo(
    () => (members.data?.items ?? []).filter((m) => m.status === "Active"),
    [members.data],
  );

  const serverByMember = useMemo(() => {
    const map = new Map<string, { breakfast: number; lunch: number; dinner: number }>();
    for (const row of meals.data?.items ?? []) {
      map.set(row.memberId, { breakfast: row.breakfast, lunch: row.lunch, dinner: row.dinner });
    }
    return map;
  }, [meals.data]);

  // Drafts hold local overrides; displayed value = draft ?? server ?? 0. Flushed debounced.
  const [drafts, setDrafts] = useState<Record<string, Draft>>({});
  const [pulse, setPulse] = useState<string | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const valueOf = useCallback(
    (memberId: string, slot: Slot) =>
      drafts[memberId]?.[slot] ?? serverByMember.get(memberId)?.[slot] ?? 0,
    [drafts, serverByMember],
  );

  const flush = useCallback(() => {
    const dirty = Object.keys(drafts);
    if (dirty.length === 0) return;
    const entries = dirty.map((memberId) => {
      const server = serverByMember.get(memberId);
      const draft = drafts[memberId];
      return {
        memberId,
        breakfast: draft?.breakfast ?? server?.breakfast ?? 0,
        lunch: draft?.lunch ?? server?.lunch ?? 0,
        dinner: draft?.dinner ?? server?.dinner ?? 0,
      };
    });
    submit.mutate(entries, {
      onSettled: () =>
        setDrafts((current) => {
          const next = { ...current };
          for (const memberId of dirty) delete next[memberId];
          return next;
        }),
    });
  }, [drafts, serverByMember, submit]);

  // Debounce stepper bursts into one mutation per settle.
  useEffect(() => {
    if (Object.keys(drafts).length === 0) return;
    timer.current = setTimeout(flush, FLUSH_DEBOUNCE_MS);
    return () => clearTimeout(timer.current!);
  }, [drafts, flush]);

  const onChange = (memberId: string, slot: Slot, next: number) => {
    const cellKey = `${memberId}:${slot}`;
    setPulse(cellKey);
    // Clear so a second change on the same cell retriggers the animation.
    setTimeout(() => setPulse((current) => (current === cellKey ? null : current)), 650);
    setDrafts((current) => ({
      ...current,
      [memberId]: {
        ...(current[memberId] ??
          serverByMember.get(memberId) ?? { breakfast: 0, lunch: 0, dinner: 0 }),
        [slot]: next,
      },
    }));
  };

  const totals = useMemo(() => {
    let breakfast = 0;
    let lunch = 0;
    let dinner = 0;
    for (const member of activeMembers) {
      breakfast += valueOf(member.memberId, "breakfast");
      lunch += valueOf(member.memberId, "lunch");
      dinner += valueOf(member.memberId, "dinner");
    }
    return { breakfast, lunch, dinner };
  }, [activeMembers, valueOf]);

  if (members.isLoading || meals.isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-10" />
        <Skeleton className="h-16" />
        <Skeleton className="h-16" />
        <Skeleton className="h-16" />
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Input
          type="date"
          value={date}
          max={todayIso()}
          onChange={(e) => {
            if (timer.current) clearTimeout(timer.current);
            setDrafts({});
            setDate(e.target.value);
          }}
          className="w-44"
          aria-label="Meal date"
        />
        <p className="text-sm text-muted-foreground">
          Counts include guest meals — add guests to the member&apos;s slot.
        </p>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="sticky left-0 z-10 bg-background">Member</TableHead>
              <TableHead>Breakfast</TableHead>
              <TableHead>Lunch</TableHead>
              <TableHead>Dinner</TableHead>
              <TableHead className="text-right">Total</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {activeMembers.map((member) => {
              const editable = isManager || member.memberId === user?.id;
              return (
                <TableRow key={member.memberId}>
                  <TableCell className="sticky left-0 z-10 bg-background font-medium">
                    {member.name}
                    {member.memberId === user?.id && (
                      <span className="ml-1 text-xs text-muted-foreground">(you)</span>
                    )}
                  </TableCell>
                  {(["breakfast", "lunch", "dinner"] as const).map((slot) => {
                    const cellKey = `${member.memberId}:${slot}`;
                    return (
                      <TableCell
                        key={slot}
                        className={pulse === cellKey ? "just-changed" : undefined}
                      >
                        {editable ? (
                          <MealStepper
                            value={valueOf(member.memberId, slot)}
                            onChange={(next) => onChange(member.memberId, slot, next)}
                          />
                        ) : (
                          <span className="num text-muted-foreground">
                            {formatCount(valueOf(member.memberId, slot))}
                          </span>
                        )}
                      </TableCell>
                    );
                  })}
                  <TableCell className="num text-right font-medium">
                    {formatCount(
                      valueOf(member.memberId, "breakfast") +
                        valueOf(member.memberId, "lunch") +
                        valueOf(member.memberId, "dinner"),
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell className="sticky left-0 z-10 bg-background font-medium">
                Day total
              </TableCell>
              <TableCell className="num font-medium">{formatCount(totals.breakfast)}</TableCell>
              <TableCell className="num font-medium">{formatCount(totals.lunch)}</TableCell>
              <TableCell className="num font-medium">{formatCount(totals.dinner)}</TableCell>
              <TableCell className="num text-right font-semibold">
                {formatCount(totals.breakfast + totals.lunch + totals.dinner)}
              </TableCell>
            </TableRow>
          </TableFooter>
        </Table>
      </div>
    </div>
  );
}
