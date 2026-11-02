"use client";

import { useTheme } from "next-themes";
import {
  BarChart3,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  LayoutDashboard,
  LogOut,
  Moon,
  Settings,
  Sun,
  Users,
  Utensils,
  Wallet,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { type ReactNode } from "react";
import { toast } from "sonner";

import { Authorize } from "@/components/authorize";
import { useCurrentMess } from "@/components/mess-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { authScreenApi } from "@/features/messes/services";
import { useMesses } from "@/features/messes/hooks";
import { useAuth } from "@/lib/auth-context";
import { clearSession } from "@/lib/auth";
import { useUiStore } from "@/lib/stores/ui";

const NAV = [
  { href: "", label: "Overview", icon: LayoutDashboard },
  { href: "/meals", label: "Meal Sheet", icon: Utensils },
  { href: "/expenses", label: "Expenses & Deposits", icon: Wallet },
  { href: "/members", label: "Members", icon: Users },
  { href: "/reports", label: "Reports", icon: BarChart3 },
];

/**
 * App shell per docs/FRONTEND_DESIGN_OVERVIEW.md §2: collapsible desktop sidebar with mess
 * switcher, top utility bar (month switcher + user menu), mobile bottom navigation.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { messId, mess, myRole } = useCurrentMess();
  const { user } = useAuth();
  const { sidebarCollapsed, toggleSidebar, activeMonth, setActiveMonth } = useUiStore();

  return (
    <div className="min-h-dvh bg-background">
      {/* Desktop sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-30 hidden flex-col border-r bg-sidebar md:flex ${
          sidebarCollapsed ? "w-16" : "w-60"
        }`}
      >
        <div className="border-b p-3">
          <MessSwitcher activeMessName={mess?.name} myRole={myRole} collapsed={sidebarCollapsed} />
        </div>
        <nav className="flex-1 space-y-1 p-2">
          {NAV.map(({ href, label, icon: Icon }) => (
            <Link
              key={href}
              href={`/dashboard/${messId}${href}`}
              className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-sidebar-foreground hover:bg-sidebar-accent"
              title={label}
            >
              <Icon className="size-4 shrink-0" />
              {!sidebarCollapsed && label}
            </Link>
          ))}
          <Authorize roles={["MealManager", "superAdmin"]}>
            <Link
              href={`/dashboard/${messId}/settings`}
              className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-sidebar-foreground hover:bg-sidebar-accent"
            >
              <Settings className="size-4 shrink-0" />
              {!sidebarCollapsed && "Settings"}
            </Link>
          </Authorize>
        </nav>
        <div className="border-b p-2">
          <Button
            variant="ghost"
            size="sm"
            className="w-full justify-start gap-3"
            onClick={toggleSidebar}
          >
            {sidebarCollapsed ? (
              <ChevronRight className="size-4" />
            ) : (
              <ChevronLeft className="size-4" />
            )}
            {!sidebarCollapsed && "Collapse"}
          </Button>
        </div>
      </aside>

      {/* Main column */}
      <div className={`flex min-h-dvh flex-col ${sidebarCollapsed ? "md:pl-16" : "md:pl-60"}`}>
        <header className="sticky top-0 z-20 flex h-14 items-center gap-3 border-b bg-background px-4">
          <span className="text-sm font-semibold md:hidden">{mess?.name}</span>
          <div className="ml-auto flex items-center gap-2">
            <MonthSwitcher
              year={activeMonth.year}
              month={activeMonth.month}
              onChange={setActiveMonth}
            />
            <ThemeToggle />
            <UserMenu name={user?.name} />
          </div>
        </header>
        <main className="flex-1 p-4 pb-24 md:p-6 md:pb-6">{children}</main>
      </div>

      {/* Mobile bottom nav (4 core tabs) */}
      <nav className="fixed inset-x-0 bottom-0 z-30 grid grid-cols-4 border-t bg-background md:hidden">
        {[
          { href: "", label: "Overview", icon: LayoutDashboard },
          { href: "/meals", label: "Meals", icon: Utensils },
          { href: "/expenses", label: "Expenses", icon: Wallet },
          { href: "/reports", label: "Summary", icon: BarChart3 },
        ].map(({ href, label, icon: Icon }) => (
          <Link
            key={href}
            href={`/dashboard/${messId}${href}`}
            className="flex flex-col items-center gap-1 py-2 text-xs text-muted-foreground hover:text-foreground"
          >
            <Icon className="size-5" />
            {label}
          </Link>
        ))}
      </nav>
    </div>
  );
}

function MessSwitcher({
  activeMessName,
  myRole,
  collapsed,
}: {
  activeMessName?: string;
  myRole: string | null;
  collapsed: boolean;
}) {
  const { data } = useMesses();
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="w-full rounded-md px-2 py-2 text-left hover:bg-sidebar-accent"
          aria-label="Switch mess"
        >
          {collapsed ? (
            <span className="font-semibold">{activeMessName?.[0] ?? "M"}</span>
          ) : (
            <>
              <span className="block truncate text-sm font-semibold">
                {activeMessName ?? "Mess"}
              </span>
              <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                {myRole && (
                  <Badge variant="secondary">{myRole === "MealManager" ? "Manager" : myRole}</Badge>
                )}
              </span>
            </>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        {data?.items.map((mess) => (
          <DropdownMenuItem key={mess.id} asChild>
            <Link href={`/dashboard/${mess.id}`}>{mess.name}</Link>
          </DropdownMenuItem>
        ))}
        <DropdownMenuItem asChild>
          <Link href="/dashboard" className="font-medium text-primary">
            + Create / Join Mess
          </Link>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function MonthSwitcher({
  year,
  month,
  onChange,
}: {
  year: number;
  month: number;
  onChange: (year: number, month: number) => void;
}) {
  const shift = (delta: number) => {
    const d = new Date(year, month - 1 + delta, 1);
    onChange(d.getFullYear(), d.getMonth() + 1);
  };

  const label = new Date(year, month - 1, 1).toLocaleString("en", {
    month: "long",
    year: "numeric",
  });

  return (
    <div className="flex items-center gap-1 rounded-md border px-1 py-0.5">
      <Button
        variant="ghost"
        size="icon"
        className="size-6"
        onClick={() => shift(-1)}
        aria-label="Previous month"
      >
        <ChevronLeft className="size-3.5" />
      </Button>
      <span className="flex items-center gap-1.5 text-sm font-medium">
        <CalendarDays className="size-3.5 text-muted-foreground" />
        {label}
      </span>
      <Button
        variant="ghost"
        size="icon"
        className="size-6"
        onClick={() => shift(1)}
        aria-label="Next month"
      >
        <ChevronRight className="size-3.5" />
      </Button>
    </div>
  );
}

function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label="Toggle theme"
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
    >
      <Sun className="size-4 dark:hidden" />
      <Moon className="hidden size-4 dark:block" />
    </Button>
  );
}

function UserMenu({ name }: { name?: string }) {
  const router = useRouter();
  const logout = async () => {
    try {
      await authScreenApi.logout();
    } finally {
      clearSession();
      toast.success("Signed out");
      router.replace("/login");
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Account">
          <span className="flex size-7 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground">
            {name?.[0]?.toUpperCase() ?? "?"}
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem disabled>{name}</DropdownMenuItem>
        <DropdownMenuItem onClick={logout} variant="destructive">
          <LogOut className="size-4" /> Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
