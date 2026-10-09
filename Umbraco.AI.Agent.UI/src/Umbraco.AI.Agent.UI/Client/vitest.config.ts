import { defineConfig } from "vitest/config";

// CI (Azure Pipelines sets TF_BUILD) also writes a JUnit report so the pipeline
// can publish frontend test results alongside the .NET ones.
const isCI = Boolean(process.env.TF_BUILD || process.env.CI);

export default defineConfig({
    test: {
        environment: "happy-dom",
        include: ["src/**/*.test.ts"],
        reporters: isCI ? ["default", "junit"] : ["default"],
        outputFile: { junit: "./test-results/junit.xml" },
    },
});
