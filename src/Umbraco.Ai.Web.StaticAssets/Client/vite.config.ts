import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  build: {
    lib: {
      // Two entry points:
      // - bundle.ts: Only exports manifests (for Umbraco's bundle loader)
      // - bundle.manifests.ts: Exports manifests + public API (for @umbraco-ai/core import map)
      entry: {
        "umbraco-ai-bundle": resolve(__dirname, "src/bundle.ts"),
        "umbraco-ai-app": resolve(__dirname, "src/app.ts"),
      },
      formats: ["es"],
    },
    outDir: "../wwwroot",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
