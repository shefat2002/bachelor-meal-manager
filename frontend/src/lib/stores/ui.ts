"use client";

import { create } from "zustand";

/** Client UI state (skill bmm-server-state: server state in TanStack, UI state in Zustand). */
interface UiState {
  sidebarCollapsed: boolean;
  toggleSidebar: () => void;
  /** Active month scoping every dashboard query — set via the top-bar month switcher. */
  activeMonth: { year: number; month: number }; // month is 1-based
  setActiveMonth: (year: number, month: number) => void;
}

const now = new Date();

export const useUiStore = create<UiState>((set) => ({
  sidebarCollapsed: false,
  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
  activeMonth: { year: now.getFullYear(), month: now.getMonth() + 1 },
  setActiveMonth: (year, month) => set({ activeMonth: { year, month } }),
}));
