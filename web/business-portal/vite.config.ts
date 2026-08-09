import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";

// Resolve the shared design-system package (@illumin360/ui) from its source barrel.
const ui = fileURLToPath(new URL("./packages/ui/src/index.ts", import.meta.url));

// The `/api` proxy stands in for the per-portal BFF in local dev: the SPA calls same-origin
// `/api/...`, which Vite forwards to the YARP gateway → Candidates API (live, Keycloak-secured).
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@illumin360/ui": ui },
  },
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:8088",
        changeOrigin: true,
      },
    },
  },
});
