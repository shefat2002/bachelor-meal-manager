"use client";

import { useCurrentMess } from "@/components/mess-provider";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

/** Overview dashboard — KPI cards, trend chart and activity feed land in Phase 3 (P3-FE-1/2). */
export default function MessOverviewPage() {
  const { mess, myRole } = useCurrentMess();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold">{mess?.name}</h1>
        <p className="text-sm text-muted-foreground">
          Role: {myRole === "MealManager" ? "Meal Manager" : myRole}
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {["Meal rate", "On-hand balance", "Total expenditure", "My status"].map((label) => (
          <Card key={label}>
            <CardHeader>
              <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">Arrives in Phase 3 (Orleans summary).</p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
