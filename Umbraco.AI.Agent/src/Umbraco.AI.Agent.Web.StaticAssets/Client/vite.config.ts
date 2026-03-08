import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
    esbuild: {
        jsx: "automatic",
        jsxImportSource: "react",
    },
    define: {
        "process.env.NODE_ENV": JSON.stringify("production"),
    },
    build: {
        lib: {
            entry: {
                "umbraco-ai-agent-manifests": resolve(__dirname, "src/manifests.ts"),
                "umbraco-ai-agent-app": resolve(__dirname, "src/app.ts"),
            },
            formats: ["es"],
        },
        outDir: "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            // Externalize @umbraco packages (available in backoffice runtime)
            // and @umbraco-ai packages (provided by Core via import map)
            // React, ReactDOM, and @xyflow/react are bundled (not in runtime)
            external: [/^@umbraco/, /^@umbraco-ai/],
        },
    },
});
