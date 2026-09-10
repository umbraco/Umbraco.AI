import { defineConfig } from "vitest/config";
import { resolve } from "node:path";

export default defineConfig({
    test: {
        // Tests that touch a backoffice context or controller (e.g. run-controller-interrupt-boundary.test.ts)
        // pull in @umbraco-ui/uui, which imports a bare directory (`@umbraco-ui/uui-css/lib`). Node's ESM
        // resolver rejects directory imports, so these have to be transformed by Vite rather than
        // externalised to Node -- same fix as Umbraco.AI's own vitest.config.ts.
        server: { deps: { inline: [/@umbraco-ui\//, /@umbraco-cms\//] } },
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        // `@umbraco-ai/core` has no "main"/"module" entry — only a `types` rollup — so, unlike
        // `@umbraco-ai/agent` and `@umbraco-ai/agent-ui` (whose package.json `main` points at their
        // own `src/index.ts`, letting plain node-module resolution find real TS source through the
        // npm workspace symlink), it's unresolvable by a plain module loader outside the backoffice
        // build pipeline, which externalizes it instead of bundling it (see `vite.config.ts`'s
        // `rollupOptions.external`). `@umbraco-ai/agent-ui`'s barrel transitively imports from it via
        // `@umbraco-ai/agent`'s agent-picker component, so a test importing from `@umbraco-ai/agent-ui`
        // needs the bare specifier to resolve to something. Alias it straight at core's own broad
        // internal barrel (`src/index.ts` — the same file `@umbraco-ai/agent`/`@umbraco-ai/agent-ui`
        // expose as their package.json `main`) so the bare specifier resolves the same way theirs does.
        alias: {
            "@umbraco-ai/core": resolve(
                __dirname,
                "../../../../Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/index.ts",
            ),
        },
    },
});
