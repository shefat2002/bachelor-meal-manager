import { z } from "zod";

/** Mirrors backend FluentValidation rules 1:1 (skill bmm-forms-validation). */

export const bazarItemSchema = z.object({
  name: z.string().trim().min(1, "Item name is required").max(200),
  quantity: z.coerce.number().min(0.01, "Must be > 0").max(99_999),
  price: z.coerce.number().min(0.01, "Must be > 0").max(999_999_999),
});

export const bazarSchema = z.object({
  date: z.string().min(1, "Date is required"),
  buyerMemberId: z.string().optional(),
  items: z.array(bazarItemSchema).min(1, "Add at least one item").max(50, "At most 50 items"),
});
export type BazarInput = z.infer<typeof bazarSchema>;

export const utilityTypeSchema = z.object({
  name: z.string().trim().min(1, "Name is required").max(100),
  description: z.string().trim().max(300).optional(),
});
export type UtilityTypeInput = z.infer<typeof utilityTypeSchema>;

export const utilityPaymentSchema = z.object({
  utilityTypeId: z.string().min(1, "Pick a utility type"),
  amount: z.coerce.number().min(0.01, "Must be > 0").max(999_999_999),
  paymentDate: z.string().min(1, "Date is required"),
  note: z.string().trim().max(300).optional(),
});
export type UtilityPaymentInput = z.infer<typeof utilityPaymentSchema>;

export const depositSchema = z.object({
  /** Set only by managers recording on behalf of a member (auto-approved server-side). */
  memberId: z.string().optional(),
  amount: z.coerce.number().min(0.01, "Must be > 0").max(999_999_999),
  date: z.string().min(1, "Date is required"),
  note: z.string().trim().max(300).optional(),
});
export type DepositInput = z.infer<typeof depositSchema>;
