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
      external: [/^@umbraco/, "stream", "http", "https", "url", "zlib"],
      output: {
        // Group CopilotKit's heavy dependencies into fewer chunks
        // These are lazy-loaded when sidebar opens (via dynamic import)
        manualChunks: (id) => {
          if (
            id.includes("@copilotkit") ||
            id.includes("shiki") ||
            id.includes("mermaid") ||
            id.includes("cytoscape") ||
            id.includes("react-markdown") ||
            id.includes("remark") ||
            id.includes("rehype") ||
            id.includes("streamdown")
          ) {
            return "copilot-vendor";
          }
          if (id.includes("react") || id.includes("react-dom")) {
            return "react-vendor";
          }
        },
      },
    },
  },
});
