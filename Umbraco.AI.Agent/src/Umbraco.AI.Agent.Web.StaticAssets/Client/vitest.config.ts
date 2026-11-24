import { defineConfig } from "vitest/config";
import { resolve } from "node:path";

export default defineConfig({
    test: {
        // @umbraco-ui/uui imports a bare directory, which Node's ESM resolver rejects, so it and the
        // backoffice have to be transformed by Vite rather than externalised.
        server: { deps: { inline: [/@umbraco-ui\//, /@umbraco-cms\//] } },
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        // `@umbraco-ai/core` has no runtime entry in node resolution (only a types rollup), so alias it
        // to its public `exports.ts`, the same file the import map serves at runtime through `app.ts`.
        alias: {
            "@umbraco-ai/core": resolve(
                __dirname,
                "../../../../Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/exports.ts",
            ),
        },
    },
});
