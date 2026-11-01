export default function Home() {
  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="text-3xl font-semibold">Bachelor Meal Manager</h1>
      <p className="mt-2 text-neutral-500">Shared-mess meal, bazar and settlement management.</p>
      <p className="mt-4 text-sm text-neutral-400">
        Frontend scaffold — screens arrive with Phase 1 (auth) and Phase 2 (meals &amp; expenses).
        All data comes from the ASP.NET Core backend API; the frontend never talks to the database
        directly.
      </p>
    </main>
  );
}
