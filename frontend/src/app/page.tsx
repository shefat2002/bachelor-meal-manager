import Link from "next/link";

export default function Home() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-2xl flex-col items-center justify-center gap-6 p-8 text-center">
      <h1 className="text-3xl font-semibold">Bachelor Meal Manager</h1>
      <p className="text-muted-foreground">Shared-mess meal, bazar and settlement management.</p>
      <div className="flex gap-3">
        <Link
          href="/dashboard"
          className="rounded-md bg-primary px-4 py-2 font-medium text-primary-foreground hover:opacity-90"
        >
          Go to dashboard
        </Link>
        <Link href="/login" className="rounded-md border px-4 py-2 font-medium">
          Sign in
        </Link>
      </div>
    </main>
  );
}
