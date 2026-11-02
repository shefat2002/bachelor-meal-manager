"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Trash2 } from "lucide-react";
import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import { Button } from "@/components/ui/button";
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
import { useCreateBazar } from "@/features/expenses/hooks";
import { bazarSchema, type BazarInput } from "@/features/expenses/schemas";
import { useCurrentMess } from "@/components/mess-provider";
import { useMembers } from "@/features/messes/hooks";
import { useAuth } from "@/lib/auth-context";
import { formatMoney, todayIso } from "@/lib/format";

/** `+ Add Bazar` drawer: dynamic item rows (name/qty/unit price) with live auto-sum — never type a total. */
export function AddBazarSheet({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { messId } = useCurrentMess();
  const { user } = useAuth();
  const members = useMembers(messId);
  const createBazar = useCreateBazar(messId);

  const activeMembers = (members.data?.items ?? []).filter((m) => m.status === "Active");

  const {
    register,
    control,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<z.input<typeof bazarSchema>, unknown, BazarInput>({
    resolver: zodResolver(bazarSchema),
    defaultValues: {
      date: todayIso(),
      buyerMemberId: undefined,
      items: [{ name: "", quantity: 1, price: undefined }],
    },
  });

  useEffect(() => {
    if (user) setValue("buyerMemberId", user.id);
  }, [user, setValue]);

  const { fields, append, remove } = useFieldArray({ control, name: "items" });
  const items = watch("items");

  const total = (items ?? []).reduce(
    (sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.price) || 0),
    0,
  );

  const onSubmit = handleSubmit(async (input) => {
    await createBazar.mutateAsync(input);
    toast.success("Bazar expenses recorded");
    onOpenChange(false);
    reset({
      date: todayIso(),
      buyerMemberId: user?.id,
      items: [{ name: "", quantity: 1, price: undefined }],
    });
  });

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="flex w-full flex-col sm:max-w-md">
        <SheetHeader>
          <SheetTitle>Add bazar expenses</SheetTitle>
          <SheetDescription>One row per item — the total sums itself.</SheetDescription>
        </SheetHeader>

        <form className="flex flex-1 flex-col gap-4 overflow-y-auto px-4" onSubmit={onSubmit}>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="bazar-date">Date</Label>
              <Input id="bazar-date" type="date" max={todayIso()} {...register("date")} />
              {errors.date && <p className="text-sm text-arrear">{errors.date.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="bazar-buyer">Buyer</Label>
              <Select onValueChange={(v) => setValue("buyerMemberId", v)} defaultValue={user?.id}>
                <SelectTrigger id="bazar-buyer" className="w-full">
                  <SelectValue placeholder="Who shopped?" />
                </SelectTrigger>
                <SelectContent>
                  {activeMembers.map((m) => (
                    <SelectItem key={m.memberId} value={m.memberId}>
                      {m.name}
                      {m.memberId === user?.id ? " (you)" : ""}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-3">
            {fields.map((field, index) => (
              <div key={field.id} className="space-y-2 rounded-lg border p-3">
                <div className="space-y-2">
                  <Label htmlFor={`item-${index}-name`}>Item {index + 1}</Label>
                  <Input
                    id={`item-${index}-name`}
                    placeholder="Rice, eggs, vegetables…"
                    {...register(`items.${index}.name`)}
                  />
                  {errors.items?.[index]?.name && (
                    <p className="text-sm text-arrear">{errors.items[index]?.name?.message}</p>
                  )}
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div className="space-y-2">
                    <Label htmlFor={`item-${index}-qty`}>Quantity</Label>
                    <Input
                      id={`item-${index}-qty`}
                      type="number"
                      step="0.01"
                      min="0.01"
                      {...register(`items.${index}.quantity`)}
                    />
                    {errors.items?.[index]?.quantity && (
                      <p className="text-sm text-arrear">
                        {errors.items[index]?.quantity?.message}
                      </p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`item-${index}-price`}>Unit price</Label>
                    <Input
                      id={`item-${index}-price`}
                      type="number"
                      step="0.01"
                      min="0.01"
                      {...register(`items.${index}.price`)}
                    />
                    {errors.items?.[index]?.price && (
                      <p className="text-sm text-arrear">{errors.items[index]?.price?.message}</p>
                    )}
                  </div>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">
                    Line:{" "}
                    <span className="num">
                      {formatMoney(
                        (Number(items?.[index]?.quantity) || 0) *
                          (Number(items?.[index]?.price) || 0),
                      )}
                    </span>
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="xs"
                    className="text-arrear"
                    disabled={fields.length <= 1}
                    onClick={() => remove(index)}
                  >
                    <Trash2 className="size-3.5" /> Remove
                  </Button>
                </div>
              </div>
            ))}
            {errors.items?.message && errors.items?.type === "too_small" && (
              <p className="text-sm text-arrear">{errors.items.message}</p>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => append({ name: "", quantity: 1, price: undefined })}
            >
              <Plus className="size-4" /> Add row
            </Button>
          </div>
        </form>

        <SheetFooter className="mt-auto flex-col gap-3 border-t px-4">
          <div className="flex w-full items-center justify-between text-sm font-medium">
            <span>Total</span>
            <span className="num text-base font-semibold">{formatMoney(total)}</span>
          </div>
          <Button type="button" className="w-full" disabled={isSubmitting} onClick={onSubmit}>
            {isSubmitting ? "Saving…" : "Save expenses"}
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
