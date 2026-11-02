"use client";

import { Authorize } from "@/components/authorize";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useCurrentMess } from "@/components/mess-provider";
import { useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { messKeys } from "@/features/messes/hooks";
import { toast } from "sonner";
import { useState } from "react";

/** Mess settings — manager-gated; rename for now, more config in later phases. */
export default function SettingsPage() {
  const { messId, mess } = useCurrentMess();
  const queryClient = useQueryClient();
  const [name, setName] = useState(mess?.name ?? "");
  const [saving, setSaving] = useState(false);

  const save = async () => {
    setSaving(true);
    try {
      await apiClient.put(`/api/messes/${messId}`, { name });
      await queryClient.invalidateQueries({ queryKey: messKeys.all });
      toast.success("Mess updated");
    } catch {
      toast.error("Could not update the mess.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Authorize
      roles={["MealManager", "superAdmin"]}
      fallback={
        <Card>
          <CardContent className="py-10 text-center text-sm text-muted-foreground">
            Only mess managers can change settings.
          </CardContent>
        </Card>
      }
    >
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Mess settings</CardTitle>
        </CardHeader>
        <CardContent className="flex max-w-sm gap-2">
          <Input value={name} onChange={(e) => setName(e.target.value)} />
          <Button onClick={save} disabled={saving || !name.trim() || name === mess?.name}>
            Save
          </Button>
        </CardContent>
      </Card>
    </Authorize>
  );
}
