"use client";

import { HandCoins, ShoppingCart, Zap } from "lucide-react";

import { BazarTab } from "@/features/expenses/components/bazar-tab";
import { DepositsTab } from "@/features/expenses/components/deposits-tab";
import { UtilitiesTab } from "@/features/expenses/components/utilities-tab";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

/** Expenses & deposits hub — three-tab layout (§3.3 FRONTEND_DESIGN_OVERVIEW), scoped by the month switcher. */
export default function ExpensesPage() {
  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">Expenses &amp; deposits</h1>
      <Tabs defaultValue="bazar">
        <TabsList>
          <TabsTrigger value="bazar">
            <ShoppingCart className="size-4" /> Bazar
          </TabsTrigger>
          <TabsTrigger value="utilities">
            <Zap className="size-4" /> Utilities
          </TabsTrigger>
          <TabsTrigger value="deposits">
            <HandCoins className="size-4" /> Deposits
          </TabsTrigger>
        </TabsList>
        <TabsContent value="bazar">
          <BazarTab />
        </TabsContent>
        <TabsContent value="utilities">
          <UtilitiesTab />
        </TabsContent>
        <TabsContent value="deposits">
          <DepositsTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
