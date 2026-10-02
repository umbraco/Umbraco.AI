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
import type { UaiBinaryDecisionResult, UaiDecisionQuestion, UaiDecisionResult } from "../types.js";

function createHost(): UmbControllerHost {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    return host;
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
                data: { kind: "binary", trueProbability: 0.97, modelId: "jev-latest" },
            });
            const controller = new UaiDecisionController(createHost());
            result = await controller.ask({ kind: "binary", instructions: "Is this spam?" });
        });

        it("returns a binary result with the true probability", () => {
            expect(result.data).toMatchObject({ kind: "binary", trueProbability: 0.97 });
        });
    });

    describe("Scenario: a caller holds a question typed only as the UaiDecisionQuestion union", () => {
        let result: { data?: UaiDecisionResult; error?: unknown };

        beforeEach(async () => {
            ask.mockResolvedValue({
                data: { kind: "binary", trueProbability: 0.97 },
            });
            const controller = new UaiDecisionController(createHost());
            const question = asUnionTypedQuestion({ kind: "binary", instructions: "Is this spam?" });
            result = await controller.ask(question);
        });

        it("resolves with a result", () => {
            expect(result.data).toMatchObject({ kind: "binary", trueProbability: 0.97 });
        });
    });

    describe("Scenario: options are passed alongside a question", () => {
        beforeEach(async () => {
            ask.mockReset();
            ask.mockResolvedValue({ data: { kind: "binary", trueProbability: 0.9 } });
            const controller = new UaiDecisionController(createHost());
            const controllerSignal = new AbortController().signal;
            await controller.ask(
                { kind: "binary", instructions: "Is this spam?" },
                { state: "Buy cheap watches", profileIdOrAlias: "spam-check", signal: controllerSignal },
            );
        });

        it("forwards the state to the repository", () => {
            expect(ask.mock.calls[0][0].state).toBe("Buy cheap watches");
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
