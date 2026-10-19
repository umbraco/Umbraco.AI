import { describe, expect, it } from "vitest";
import { computeMeanWeightedScore, computeWeightedScore } from "./weighted-score.js";

describe("Feature: weighted score", () => {
    describe("Scenario: a single run", () => {
        it("weights each grader score by its weight", () => {
            const score = computeWeightedScore([
                { score: 1, weight: 3 },
                { score: 0, weight: 1 },
            ]);
            expect(score).toBeCloseTo(0.75);
        });

        it("is null when no grader ran", () => {
            expect(computeWeightedScore([])).toBeNull();
            expect(computeWeightedScore(undefined)).toBeNull();
        });

        it("is 0 when the total weight is 0", () => {
            expect(computeWeightedScore([{ score: 1, weight: 0 }])).toBe(0);
        });
    });

    describe("Scenario: the mean across runs", () => {
        it("averages completed runs, counting errored runs without a score as 0", () => {
            const mean = computeMeanWeightedScore([
                { status: "Passed", weightedScore: 1 },
                { status: "Failed", weightedScore: 0.5 },
                { status: "Error", weightedScore: null },
            ]);
            expect(mean).toBeCloseTo(0.5);
        });

        it("ignores runs that have not completed", () => {
            const mean = computeMeanWeightedScore([
                { status: "Passed", weightedScore: 1 },
                { status: "Running", weightedScore: null },
            ]);
            expect(mean).toBe(1);
        });

        it("is null when no run has completed", () => {
            expect(computeMeanWeightedScore([])).toBeNull();
            expect(computeMeanWeightedScore([{ status: "Running" }])).toBeNull();
        });
    });
});
