import { defineConfig } from "vitest/config";
import { resolve } from "node:path";

export default defineConfig({
    test: {
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        // `@umbraco-ai/core` (and, transitively through it, parts of `@umbraco-ai/agent`'s barrel) has
        // no "main"/"module" entry — only a `types` rollup — so it's unresolvable by a plain module
        // loader outside the backoffice build pipeline, which externalizes it instead of bundling it
        // (see `vite.config.ts`'s `rollupOptions.external`). Test-only aliases to local stubs let a test
        // import a real class from `@umbraco-ai/agent-ui`/`@umbraco-ai/agent` without the transform
        // failing on that unrelated, unresolvable transitive import. Never applied to the real build.
        alias: {
            "@umbraco-ai/core": resolve(__dirname, "src/test-stubs/umbraco-ai-core.stub.ts"),
            "@umbraco-ai/agent": resolve(__dirname, "src/test-stubs/umbraco-ai-agent.stub.ts"),
        },
    },
});
