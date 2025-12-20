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
      external: [/^@umbraco/, /^@ag-ui/],
    },
  },
});
