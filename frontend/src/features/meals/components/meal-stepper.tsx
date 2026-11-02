"use client";

import { Minus, Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import { formatCount } from "@/lib/format";

const STEP = 0.5;
const MAX = 10;

/** `− 0 ½ 1 2 +` quick control for one meal slot (0–10 in 0.5 steps; guest meals fold into the counts). */
export function MealStepper({
  value,
  disabled,
  onChange,
}: {
  value: number;
  disabled?: boolean;
  onChange: (next: number) => void;
}) {
  const clamp = (n: number) => Math.max(0, Math.min(MAX, Math.round(n * 2) / 2));

  return (
    <div className="flex items-center gap-1">
      <Button
        type="button"
        variant="outline"
        size="icon-xs"
        aria-label="Decrease"
        disabled={disabled || value <= 0}
        onClick={() => onChange(clamp(value - STEP))}
      >
        <Minus className="size-3" />
      </Button>
      <span className="num w-7 text-center text-sm font-medium">{formatCount(value)}</span>
      <Button
        type="button"
        variant="outline"
        size="icon-xs"
        aria-label="Increase"
        disabled={disabled || value >= MAX}
        onClick={() => onChange(clamp(value + STEP))}
      >
        <Plus className="size-3" />
      </Button>
      <span className="ml-1 hidden gap-0.5 sm:flex">
        {[0, 0.5, 1, 2].map((preset) => (
          <Button
            key={preset}
            type="button"
            variant={value === preset ? "secondary" : "ghost"}
            size="xs"
            className="num px-1.5"
            disabled={disabled}
            onClick={() => onChange(preset)}
          >
            {formatCount(preset)}
          </Button>
        ))}
      </span>
    </div>
  );
}
