import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "umbraco-ai-manifests": resolve(__dirname, "src/bundle.manifests.ts"),
        "umbraco-ai-api": resolve(__dirname, "src/bundle.api.ts"),
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
