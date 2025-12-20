import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/bundle.manifests.ts",
      formats: ["es"],
      fileName: "umbraco-ai-agent",
    },
    outDir: "../wwwroot",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      // Only externalize @umbraco packages (available in backoffice runtime)
      // @ag-ui packages must be bundled as they're not provided by the runtime
      external: [/^@umbraco/],
    },
  },
});
