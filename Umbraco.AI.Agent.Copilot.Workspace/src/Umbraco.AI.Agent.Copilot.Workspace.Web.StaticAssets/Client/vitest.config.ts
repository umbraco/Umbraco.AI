import { defineConfig } from "vitest/config";
import { resolve } from "node:path";

export default defineConfig({
    test: {
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        // `@umbraco-ai/core` has no "main"/"module" entry — only a `types` rollup — so it's
        // unresolvable by a plain module loader outside the backoffice build pipeline, which
        // externalizes it instead of bundling it (see `vite.config.ts`'s `rollupOptions.external`).
        // `@umbraco-ai/agent-ui`'s barrel transitively imports from it via `@umbraco-ai/agent`'s
        // agent-picker component, so a test importing from `@umbraco-ai/agent-ui` needs the bare
        // specifier to resolve to something. See `src/test-stubs/umbraco-ai-core.stub.ts` — it's
        // deliberately an empty module. Never applied to the real build.
        alias: {
            "@umbraco-ai/core": resolve(__dirname, "src/test-stubs/umbraco-ai-core.stub.ts"),
        },
    },
});
