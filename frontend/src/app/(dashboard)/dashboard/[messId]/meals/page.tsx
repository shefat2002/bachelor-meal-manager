"use client";

import { CalendarDays, Grid3X3 } from "lucide-react";

import { DailyMealSheet } from "@/features/meals/components/daily-meal-sheet";
import { MonthlyMealMatrix } from "@/features/meals/components/monthly-meal-matrix";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

/** Meal sheet — dual view: daily entry (default) + monthly matrix (§3.2 FRONTEND_DESIGN_OVERVIEW). */
export default function MealsPage() {
  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">Meal sheet</h1>
      <Tabs defaultValue="daily">
        <TabsList>
          <TabsTrigger value="daily">
            <CalendarDays className="size-4" /> Daily entry
          </TabsTrigger>
          <TabsTrigger value="monthly">
            <Grid3X3 className="size-4" /> Monthly matrix
          </TabsTrigger>
        </TabsList>
        <TabsContent value="daily">
          <DailyMealSheet />
        </TabsContent>
        <TabsContent value="monthly">
          <MonthlyMealMatrix />
        </TabsContent>
      </Tabs>
    </div>
  );
}
