import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
    build: {
        lib: {
            entry: {
                "umbraco-ai-agent-copilot-workspace-manifests": resolve(__dirname, "src/manifests.ts"),
                "umbraco-ai-agent-copilot-workspace-app": resolve(__dirname, "src/app.ts"),
            },
            formats: ["es"],
        },
        outDir: "../wwwroot",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            // Externalize @umbraco packages (available in the backoffice runtime) and @umbraco-ai
            // packages (provided by Core/Agent/Agent.UI via the runtime import map).
            external: [/^@umbraco/, /^@umbraco-ai/],
        },
    },
});
