import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "umbraco-ai-agent-manifests": resolve(__dirname, "src/bundle.manifests.ts"),
        "umbraco-ai-agent-api": resolve(__dirname, "src/bundle.api.ts"),
      },
      formats: ["es"],
    },
    outDir: "../wwwroot",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      // Externalize @umbraco packages (available in backoffice runtime)
      // and @umbraco-ai packages (provided by Core via import map)
      // @ag-ui packages must be bundled as they're not provided by the runtime
      external: [/^@umbraco/, /^@umbraco-ai/],
    },
  },
});
