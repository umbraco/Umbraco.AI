// DR-5 — Ask decisions from TypeScript (AC1-AC3, AC7)
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the repository the public controller delegates to.
const ask = vi.fn();
vi.mock("../repository/decision.repository.js", () => ({
    UaiDecisionRepository: class {
        ask = ask;
    },
}));

import { UaiDecisionController } from "./decision.controller.js";
import type {
    UaiBinaryDecisionResult,
    UaiChoiceDecisionResult,
    UaiDecisionQuestion,
    UaiDecisionResult,
    UaiScoreDecisionResult,
} from "../types.js";

const TEST_HOST_TAG = "uai-decision-controller-test-host";
if (!customElements.get(TEST_HOST_TAG)) {
    customElements.define(TEST_HOST_TAG, class extends UmbControllerHostElementMixin(HTMLElement) {});
}

function createHost(): UmbControllerHost {
    const element = document.createElement(TEST_HOST_TAG);
    document.body.appendChild(element);
    return element as unknown as UmbControllerHost;
}

// Returns a value typed only as the union, never narrowed to a specific `kind` literal, so a
// caller holding this can only be served by the union overload — not one of the concrete ones.
function asUnionTypedQuestion(question: UaiDecisionQuestion): UaiDecisionQuestion {
    return question;
}

describe("Feature: UaiDecisionController", () => {
    describe("Scenario: a binary question is answered", () => {
        let result: { data?: UaiBinaryDecisionResult; error?: unknown };

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "binary", answer: true, probability: 0.97, confidence: 0.97, modelId: "jev-latest" },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({ kind: "binary", instructions: "Is this spam?" });
        });

        it("returns a binary result with the answer", () => {
            expect(result.data).toMatchObject({ kind: "binary", answer: true, probability: 0.97, confidence: 0.97 });
        });
    });

    describe("Scenario: a choice question is answered", () => {
        let result: { data?: UaiChoiceDecisionResult; error?: unknown };

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

        it("returns a choice result with the chosen key", () => {
            expect(result.data).toMatchObject({ kind: "choice", choice: "b", confidence: 0.9 });
        });
    });

    describe("Scenario: a score question is answered", () => {
        let result: { data?: UaiScoreDecisionResult; error?: unknown };

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "score", score: 1.8, level: "good", confidence: 0.8, probabilities: { good: 0.8 } },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({ kind: "score", instructions: "Rate it", levels: ["poor", "ok", "good"] });
        });

        it("returns a score result with score and level", () => {
            expect(result.data).toMatchObject({ kind: "score", score: 1.8, level: "good" });
        });
    });

    describe("Scenario: a caller holds a question typed only as the UaiDecisionQuestion union", () => {
        let result: { data?: UaiDecisionResult; error?: unknown };

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "binary", answer: true, probability: 0.97, confidence: 0.97 },
            });
            const controller = new UaiDecisionController(createHost());
            const question = asUnionTypedQuestion({ kind: "binary", instructions: "Is this spam?" });
            result = await controller.ask(question);
        });

        it("resolves with a result", () => {
            expect(result.data).toMatchObject({ kind: "binary", answer: true });
        });
    });

    describe("Scenario: options are passed alongside a question", () => {
        beforeEach(async () => {
            ask.mockReset();
            ask.mockResolvedValue({ data: { kind: "binary", answer: true, probability: 0.9, confidence: 0.9 } });
            const controller = new UaiDecisionController(createHost());
            const controllerSignal = new AbortController().signal;
            await controller.ask(
                { kind: "binary", instructions: "Is this spam?" },
                { profileIdOrAlias: "spam-check", signal: controllerSignal },
            );
        });

        it("forwards the profile id or alias to the repository", () => {
            expect(ask.mock.calls[0][0].profileIdOrAlias).toBe("spam-check");
        });

        it("forwards the abort signal to the repository", () => {
            expect(ask.mock.calls[0][0].signal).toBeInstanceOf(AbortSignal);
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the server returns 404 because Decision is disabled", () => {
            let result: { data?: UaiBinaryDecisionResult; error?: unknown };

            beforeEach(async () => {
                ask.mockResolvedValue({ error: { status: 404 } });
                const controller = new UaiDecisionController(createHost());
                result = await controller.ask({ kind: "binary", instructions: "Is this spam?" });
            });

            it("resolves with an error and no data instead of throwing", () => {
                expect(result).toEqual({ error: { status: 404 } });
            });
        });
    });
});
