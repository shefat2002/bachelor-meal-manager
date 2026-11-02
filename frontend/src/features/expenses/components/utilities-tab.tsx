"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Receipt, Zap } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
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
  useCreateUtilityPayment,
  useCreateUtilityType,
  useUpdateUtilityType,
  useUtilityPayments,
  useUtilityTypes,
} from "@/features/expenses/hooks";
import {
  utilityPaymentSchema,
  utilityTypeSchema,
  type UtilityPaymentInput,
  type UtilityTypeInput,
} from "@/features/expenses/schemas";
import { formatDay, formatMoney, monthRange, todayIso } from "@/lib/format";
import { useUiStore } from "@/lib/stores/ui";

/** Utilities tab: type cards with paid/pending status for the active month (derived from payments). */
export function UtilitiesTab() {
  const { messId } = useCurrentMess();
  const activeMonth = useUiStore((s) => s.activeMonth);
  const types = useUtilityTypes(messId);
  const [typeDialogOpen, setTypeDialogOpen] = useState(false);
  const [paymentFor, setPaymentFor] = useState<string | null>(null);

  const { from, to } = monthRange(activeMonth.year, activeMonth.month);
  const payments = useUtilityPayments(messId, { from, to, page: 1, pageSize: 100 });

  // Derived month status: paid iff at least one payment exists in the window (P2-BE-4 decision).
  const paidByType = useMemo(() => {
    const map = new Map<string, { total: number; last: string }>();
    for (const p of payments.data?.items ?? []) {
      const current = map.get(p.utilityTypeId);
      map.set(p.utilityTypeId, {
        total: (current?.total ?? 0) + p.amount,
        last: !current || p.paymentDate > current.last ? p.paymentDate : current.last,
      });
    }
    return map;
  }, [payments.data]);

  if (types.isLoading) {
    return (
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Skeleton className="h-32" />
        <Skeleton className="h-32" />
        <Skeleton className="h-32" />
      </div>
    );
  }

  const cards = types.data?.items ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-end">
        <Authorize roles={["MealManager", "superAdmin"]}>
          <Button variant="outline" size="sm" onClick={() => setTypeDialogOpen(true)}>
            <Plus className="size-4" /> Utility type
          </Button>
        </Authorize>
      </div>

      {cards.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-10 text-center text-sm text-muted-foreground">
            <Zap className="size-6" />
            No utility types yet — add Electricity, Wi-Fi, Cook Salary…
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {cards.map((type) => {
            const paid = paidByType.get(type.id);
            return (
              <Card key={type.id} className={type.isActive ? undefined : "opacity-60"}>
                <CardContent className="space-y-2 py-4">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="font-medium">{type.name}</p>
                      {type.description && (
                        <p className="text-xs text-muted-foreground">{type.description}</p>
                      )}
                    </div>
                    {!type.isActive ? (
                      <Badge variant="secondary">Inactive</Badge>
                    ) : paid ? (
                      <span className="badge-surplus">Paid</span>
                    ) : (
                      <span className="badge-pending">Pending</span>
                    )}
                  </div>
                  {paid && (
                    <p className="text-sm text-muted-foreground">
                      <span className="num font-medium text-foreground">
                        {formatMoney(paid.total)}
                      </span>{" "}
                      paid · <span className="num">{formatDay(paid.last)}</span>
                    </p>
                  )}
                  <Authorize roles={["MealManager", "superAdmin"]}>
                    <div className="flex gap-2 pt-1">
                      <Button size="xs" variant="outline" onClick={() => setPaymentFor(type.id)}>
                        <Receipt className="size-3.5" /> Record payment
                      </Button>
                      {type.isActive && (
                        <DeactivateButton utilityTypeId={type.id} name={type.name} />
                      )}
                    </div>
                  </Authorize>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      <AddUtilityTypeDialog open={typeDialogOpen} onOpenChange={setTypeDialogOpen} />
      <RecordUtilityPaymentSheet
        utilityTypeId={paymentFor}
        onOpenChange={(open) => !open && setPaymentFor(null)}
      />
    </div>
  );
}

function DeactivateButton({ utilityTypeId, name }: { utilityTypeId: string; name: string }) {
  const { messId } = useCurrentMess();
  const update = useUpdateUtilityType(messId);
  return (
    <Button
      size="xs"
      variant="ghost"
      disabled={update.isPending}
      onClick={() => update.mutate({ id: utilityTypeId, name, isActive: false })}
    >
      Deactivate
    </Button>
  );
}

function AddUtilityTypeDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { messId } = useCurrentMess();
  const create = useCreateUtilityType(messId);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<UtilityTypeInput>({ resolver: zodResolver(utilityTypeSchema) });

  const onSubmit = handleSubmit(async (input) => {
    await create.mutateAsync(input);
    toast.success("Utility type added");
    onOpenChange(false);
    reset();
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New utility type</DialogTitle>
          <DialogDescription>e.g. Electricity, Wi-Fi, Cook Salary</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="utility-name">Name</Label>
            <Input id="utility-name" placeholder="Electricity" {...register("name")} />
            {errors.name && <p className="text-sm text-arrear">{errors.name.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="utility-desc">Description (optional)</Label>
            <Input id="utility-desc" {...register("description")} />
            {errors.description && (
              <p className="text-sm text-arrear">{errors.description.message}</p>
            )}
          </div>
          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Adding…" : "Add type"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function RecordUtilityPaymentSheet({
  utilityTypeId,
  onOpenChange,
}: {
  utilityTypeId: string | null;
  onOpenChange: (open: boolean) => void;
}) {
  const { messId } = useCurrentMess();
  const types = useUtilityTypes(messId);
  const create = useCreateUtilityPayment(messId);
  const [selected, setSelected] = useState<string | null>(null);

  const effectiveType = utilityTypeId ?? selected;

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<z.input<typeof utilityPaymentSchema>, unknown, UtilityPaymentInput>({
    resolver: zodResolver(utilityPaymentSchema),
    defaultValues: { utilityTypeId: "", amount: undefined, paymentDate: todayIso() },
  });

  // The card that opened this sheet preselects its type — sync it into the form field.
  useEffect(() => {
    if (utilityTypeId) setValue("utilityTypeId", utilityTypeId);
  }, [utilityTypeId, setValue]);

  const onSubmit = handleSubmit(async (input) => {
    await create.mutateAsync(input);
    toast.success("Utility payment recorded");
    onOpenChange(false);
    reset({ utilityTypeId: "", amount: undefined, paymentDate: todayIso() });
  });

  // The sheet opens when a card picked a type, or stays closed until the manager picks one.
  const open = effectiveType !== null;

  return (
    <Sheet
      open={open}
      onOpenChange={(o) => {
        if (!o) {
          onOpenChange(false);
          setSelected(null);
        }
      }}
    >
      <SheetContent side="right" className="w-full sm:max-w-sm">
        <SheetHeader>
          <SheetTitle>Record utility payment</SheetTitle>
          <SheetDescription>Bills the active mess only.</SheetDescription>
        </SheetHeader>
        <form className="flex flex-col gap-4 px-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="payment-type">Utility type</Label>
            <Select
              value={effectiveType ?? undefined}
              onValueChange={(v) => {
                setSelected(v);
                setValue("utilityTypeId", v);
              }}
            >
              <SelectTrigger id="payment-type" className="w-full">
                <SelectValue placeholder="Pick a type" />
              </SelectTrigger>
              <SelectContent>
                {(types.data?.items ?? []).map((t) => (
                  <SelectItem key={t.id} value={t.id}>
                    {t.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.utilityTypeId && (
              <p className="text-sm text-arrear">{errors.utilityTypeId.message}</p>
            )}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="payment-amount">Amount</Label>
              <Input
                id="payment-amount"
                type="number"
                step="0.01"
                min="0.01"
                {...register("amount")}
              />
              {errors.amount && <p className="text-sm text-arrear">{errors.amount.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="payment-date">Paid on</Label>
              <Input id="payment-date" type="date" {...register("paymentDate")} />
              {errors.paymentDate && (
                <p className="text-sm text-arrear">{errors.paymentDate.message}</p>
              )}
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="payment-note">Note (optional)</Label>
            <Input id="payment-note" {...register("note")} />
          </div>
          <SheetFooter className="mt-2 flex-col gap-2 border-t px-0">
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Saving…" : "Save payment"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
