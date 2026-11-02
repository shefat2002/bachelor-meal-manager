import { z } from "zod";

/** Mirrors backend FluentValidation rules 1:1 (skill bmm-forms-validation). */

export const phoneSchema = z
  .string()
  .min(1, "Phone is required")
  .transform((value) => value.replace(/[\s\-().]/g, ""))
  .refine(
    (value) => /^\+?[0-9]{8,15}$/.test(value),
    "Enter 8–15 digits, optionally starting with +",
  );

export const nameSchema = z.string().min(1, "Name is required").max(100, "Name is too long");

export const passwordSchema = z.string().min(8, "At least 8 characters").max(128);

export const loginSchema = z.object({
  phone: phoneSchema,
  password: z.string().min(1, "Password is required"),
});

export const registerSchema = z
  .object({
    name: nameSchema,
    phone: phoneSchema,
    password: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

export const otpRequestSchema = z.object({ phone: phoneSchema });

export const otpVerifySchema = z.object({
  phone: phoneSchema,
  code: z.string().regex(/^[0-9]{6}$/, "6-digit code"),
});

export type LoginInput = z.infer<typeof loginSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;
export type OtpVerifyInput = z.infer<typeof otpVerifySchema>;
