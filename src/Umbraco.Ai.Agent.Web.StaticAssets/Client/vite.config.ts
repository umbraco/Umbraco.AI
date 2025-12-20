import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [
    react({
      include: ["**/copilot/react/**/*.tsx"],
    }),
  ],
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
      // External: Umbraco backoffice + Node.js built-ins used by @segment/analytics-node
      // The Node.js modules are from CopilotKit's telemetry dependency and are safely
      // externalized since the analytics code has browser fallbacks
      external: [/^@umbraco/, "stream", "http", "https", "url", "zlib"],
      output: {
        // Control chunk splitting - bundle CopilotKit dependencies together
        manualChunks: (id) => {
          // Bundle all CopilotKit and its heavy dependencies into one chunk
          if (
            id.includes("@copilotkit") ||
            id.includes("shiki") ||
            id.includes("mermaid") ||
            id.includes("cytoscape") ||
            id.includes("react-markdown") ||
            id.includes("remark") ||
            id.includes("rehype")
          ) {
            return "copilot-vendor";
          }
          // Bundle React separately
          if (id.includes("react") || id.includes("react-dom")) {
            return "react-vendor";
          }
        },
      },
    },
  },
});
