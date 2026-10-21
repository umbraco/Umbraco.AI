// EX-7 — Extract from TypeScript (AC1)
//
// Runtime half only: the controller is reachable through the extraction barrel that the root
// src/exports.ts re-exports. The public type rollup (types/umbraco-ai-public-types.d.ts) is made
// by api-extractor during `npm run build:core`, not by vitest, so that half is checked in T11's
// build acceptance and the T14 wire task.
import { describe, expect, it } from "vitest";
import * as extraction from "./exports.js";

describe("Feature: extraction public exports", () => {
    describe("Scenario: an extension imports the controller", () => {
        it("exports UaiExtractionController", () => {
            expect(typeof extraction.UaiExtractionController).toBe("function");
        });
    });
});
