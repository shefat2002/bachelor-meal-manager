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
import { otpVerifySchema, phoneSchema, type OtpVerifyInput } from "@/features/auth/schemas";
import { authScreenApi } from "@/features/messes/services";
import { setAccessToken, setCachedUser } from "@/lib/auth";

/** OTP login: request a 6-digit code for an existing phone account, then verify it. */
export default function VerifyPage() {
  const router = useRouter();
  const [codeSent, setCodeSent] = useState(false);
  const [requesting, setRequesting] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [devCode, setDevCode] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<OtpVerifyInput>({ resolver: zodResolver(otpVerifySchema) });

  const requestCode = async () => {
    const parsed = phoneSchema.safeParse(getValues("phone"));
    if (!parsed.success) {
      toast.error(parsed.error.issues[0]?.message ?? "Enter a valid phone number.");
      return;
    }

    setRequesting(true);
    try {
      const result = await authScreenApi.requestOtp(parsed.data);
      setCodeSent(true);
      // Development returns the code (no SMS provider until Phase 4).
      setDevCode(result.devCode ?? null);
      toast.success("Code sent.", {
        description: result.devCode ? `Dev code: ${result.devCode}` : undefined,
      });
    } catch {
      toast.error("Could not send the code. Try again.");
    } finally {
      setRequesting(false);
    }
  };

  const onSubmit = handleSubmit(async (input) => {
    setVerifying(true);
    try {
      const result = await authScreenApi.verifyOtp(input.phone, input.code);
      setAccessToken(result.accessToken);
      setCachedUser(result.user);
      router.replace("/dashboard");
    } catch {
      toast.error("Invalid or expired code.");
    } finally {
      setVerifying(false);
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-xl">Sign in with OTP</CardTitle>
        <CardDescription>
          {codeSent
            ? "Enter the 6-digit code we sent."
            : "We'll text a 6-digit code to your phone."}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="phone">Phone number</Label>
            <div className="flex gap-2">
              <Input
                id="phone"
                inputMode="tel"
                placeholder="+8801XXXXXXXXX"
                {...register("phone")}
              />
              <Button
                type="button"
                variant="outline"
                onClick={requestCode}
                disabled={requesting || codeSent}
              >
                {requesting ? "…" : codeSent ? "Sent" : "Send code"}
              </Button>
            </div>
            {errors.phone && <p className="text-sm text-arrear">{errors.phone.message}</p>}
            {devCode && <p className="text-sm text-muted-foreground">Dev code: {devCode}</p>}
          </div>
          {codeSent && (
            <div className="space-y-2">
              <Label htmlFor="code">Verification code</Label>
              <Input
                id="code"
                inputMode="numeric"
                maxLength={6}
                placeholder="000000"
                className="num tracking-widest"
                {...register("code")}
              />
              {errors.code && <p className="text-sm text-arrear">{errors.code.message}</p>}
            </div>
          )}
          <Button type="submit" className="w-full" disabled={!codeSent || verifying}>
            {verifying ? "Verifying…" : "Verify & sign in"}
          </Button>
        </form>
        <p className="mt-4 text-center text-sm">
          <Link className="text-primary hover:underline" href="/login">
            Back to password sign-in
          </Link>
        </p>
      </CardContent>
    </Card>
  );
}
