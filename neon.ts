import { defineConfig } from "@neon/config/v1";

export default defineConfig({
  // Neon is used ONLY as the hosted Postgres provider for the ASP.NET Core
  // backend (EF Core/Npgsql). No Neon Auth / Realtime / Functions — identity
  // is ASP.NET Core Identity + JWT in the backend; async UI updates go
  // through the backend API (SSE/WebSocket/polling).
  branch: (branch) => {
    if (branch.isDefault) {
      // Default branch: no overrides, uses project defaults
      return {};
    }
    if (!branch.exists) {
      // New non-default branches: auto-expire
      // Run `neon checkout <name>` to create a new branch with these settings
      return { ttl: "7d" };
    }
    // Existing branch: no changes
    return {};
  },
});
