"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { registerSchema, type RegisterInput } from "@/features/auth/schemas";
import { authScreenApi } from "@/features/messes/services";
import { setAccessToken, setCachedUser } from "@/lib/auth";

export default function RegisterPage() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterInput>({ resolver: zodResolver(registerSchema) });

  const onSubmit = handleSubmit(async (input) => {
    setSubmitting(true);
    try {
      const result = await authScreenApi.register(input.name, input.phone, input.password);
      setAccessToken(result.accessToken);
      setCachedUser(result.user);
      router.replace("/dashboard");
    } catch (error) {
      const data = (
        error as { response?: { data?: { detail?: string; errors?: Record<string, string[]> } } }
      )?.response?.data;
      const firstField = data?.errors && Object.values(data.errors)[0]?.[0];
      toast.error(firstField ?? data?.detail ?? "Registration failed.");
    } finally {
      setSubmitting(false);
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-xl">Create account</CardTitle>
        <CardDescription>
          Register with your phone number — you can create or join a mess after.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="name">Full name</Label>
            <Input id="name" placeholder="Your name" {...register("name")} />
            {errors.name && <p className="text-sm text-arrear">{errors.name.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="phone">Phone number</Label>
            <Input id="phone" inputMode="tel" placeholder="+8801XXXXXXXXX" {...register("phone")} />
            {errors.phone && <p className="text-sm text-arrear">{errors.phone.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input id="password" type="password" {...register("password")} />
            {errors.password && <p className="text-sm text-arrear">{errors.password.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm password</Label>
            <Input id="confirmPassword" type="password" {...register("confirmPassword")} />
            {errors.confirmPassword && (
              <p className="text-sm text-arrear">{errors.confirmPassword.message}</p>
            )}
          </div>
          <Button type="submit" className="w-full" disabled={submitting}>
            {submitting ? "Creating…" : "Create account"}
          </Button>
        </form>
        <p className="mt-4 text-center text-sm">
          <Link className="text-primary hover:underline" href="/login">
            Already have an account? Sign in
          </Link>
        </p>
      </CardContent>
    </Card>
  );
}
