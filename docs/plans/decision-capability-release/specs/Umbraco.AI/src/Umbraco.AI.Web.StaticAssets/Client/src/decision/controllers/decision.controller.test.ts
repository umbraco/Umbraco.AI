// DR-5 — Ask decisions from TypeScript (AC1-AC3, AC7)
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the repository the public controller delegates to.
const ask = vi.fn();
vi.mock("../repository/decision.repository.js", () => ({
    UaiDecisionRepository: class {
        ask = ask;
    },
}));

import { UaiDecisionController } from "./decision.controller.js";

function createHost(): UmbControllerHost {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    return host;
}

describe("Feature: UaiDecisionController", () => {
    describe("Scenario: a binary question is answered", () => {
        let result: Awaited<ReturnType<UaiDecisionController["ask"]>>;

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "binary", answer: true, probability: 0.97, confidence: 0.97, modelId: "jev-latest" },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({ kind: "binary", instructions: "Is this spam?" });
        });

        // Pending T14
        it.skip("returns a binary result with the answer", () => {
            expect(result.data).toMatchObject({ kind: "binary", answer: true, probability: 0.97, confidence: 0.97 });
        });
    });

    describe("Scenario: a choice question is answered", () => {
        let result: Awaited<ReturnType<UaiDecisionController["ask"]>>;

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "choice", choice: "b", confidence: 0.9, probabilities: { a: 0.1, b: 0.9 } },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({
                kind: "choice",
                instructions: "Pick one",
                options: [{ key: "a" }, { key: "b" }],
            });
        });

        // Pending T14
        it.skip("returns a choice result with the chosen key", () => {
            expect(result.data).toMatchObject({ kind: "choice", choice: "b", confidence: 0.9 });
        });
    });

    describe("Scenario: a score question is answered", () => {
        let result: Awaited<ReturnType<UaiDecisionController["ask"]>>;

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "score", score: 1.8, level: "good", confidence: 0.8, probabilities: { good: 0.8 } },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({ kind: "score", instructions: "Rate it", levels: ["poor", "ok", "good"] });
        });

        // Pending T14
        it.skip("returns a score result with score and level", () => {
            expect(result.data).toMatchObject({ kind: "score", score: 1.8, level: "good" });
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the server returns 404 because Decision is disabled", () => {
            let result: Awaited<ReturnType<UaiDecisionController["ask"]>>;

            beforeEach(async () => {
                ask.mockResolvedValue({ error: { status: 404 } });
                const controller = new UaiDecisionController(createHost());
                result = await controller.ask({ kind: "binary", instructions: "Is this spam?" });
            });

            // Pending T14
            it.skip("resolves with an error and no data instead of throwing", () => {
                expect(result).toEqual({ error: { status: 404 } });
            });
        });
    });
});
