import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "umbraco-ai-agent-copilot-manifests": resolve(__dirname, "src/manifests.ts"),
        "umbraco-ai-agent-copilot-app": resolve(__dirname, "src/app.ts"),
      }, 
      formats: ["es"],
    },
    outDir: "../wwwroot",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      // Externalize @umbraco packages (available in backoffice runtime)
      // and @umbraco-ai packages (provided by Agent and Core via import map)
      // @ag-ui packages must be bundled as they're not provided by the runtime
      external: [/^@umbraco/, /^@umbraco-ai/],
    },
  },
});
