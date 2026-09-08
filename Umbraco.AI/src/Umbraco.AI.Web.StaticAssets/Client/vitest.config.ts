import { defineConfig } from "vitest/config";

// CI (Azure Pipelines sets TF_BUILD) also writes a JUnit report so the pipeline
// can publish frontend test results alongside the .NET ones.
const isCI = Boolean(process.env.TF_BUILD || process.env.CI);

export default defineConfig({
    test: {
        // Tests that touch a backoffice context or controller pull in @umbraco-ui/uui, which imports
        // a bare directory (`@umbraco-ui/uui-css/lib`). Node's ESM resolver rejects directory
        // imports, so these have to be transformed by Vite rather than externalised to Node.
        server: { deps: { inline: [/@umbraco-ui\//, /@umbraco-cms\//] } },
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        reporters: isCI ? ["default", "junit"] : ["default"],
        outputFile: { junit: "./test-results/junit.xml" },
    },
});
