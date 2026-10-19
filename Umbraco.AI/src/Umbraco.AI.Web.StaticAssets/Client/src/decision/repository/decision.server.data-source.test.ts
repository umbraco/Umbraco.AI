// DR-5 — Ask decisions from TypeScript (AC1-AC3, AC7)
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the generated OpenAPI SDK call.
// ASSUMPTION: T13's regenerated client exposes `DecisionService.ask`; rename here if the generator
// names it differently.
const sdkAsk = vi.fn();
vi.mock("../../api/sdk.gen.js", () => ({
    DecisionService: { ask: (...args: unknown[]) => sdkAsk(...args) },
}));
vi.mock("@umbraco-cms/backoffice/resources", () => ({
    tryExecute: async (_host: unknown, promise: Promise<unknown>) => promise,
}));

import { UaiDecisionServerDataSource } from "./decision.server.data-source.js";

function createHost(): UmbControllerHost {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    return host;
}

describe("Feature: decision server data source", () => {
    describe("Scenario: a choice question is sent with a profile alias", () => {
        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "choice", choice: "a", confidence: 0.6, probabilities: {} } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            await dataSource.ask({
                question: { kind: "choice", instructions: "Pick one", options: [{ key: "a" }, { key: "b" }] },
                profileIdOrAlias: "spam-check",
            });
        });

        it("sends the question kind as the API's $type", () => {
            expect(sdkAsk.mock.calls[0][0].body.question.$type).toBe("choice");
        });

        it("forwards the profile id or alias", () => {
            expect(sdkAsk.mock.calls[0][0].body.profileIdOrAlias).toBe("spam-check");
        });
    });

    describe("Scenario: a response comes back from the API", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "binary", trueProbability: 0.2 } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({ question: { kind: "binary", instructions: "Is this spam?" } });
        });

        it("maps the API's $type back to kind", () => {
            expect(result.data?.kind).toBe("binary");
        });
    });

    describe("Scenario: the server returns a response with an unrecognized $type", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "ranking", confidence: 0.5 } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({ question: { kind: "binary", instructions: "Is this spam?" } });
        });

        it("returns an explicit error instead of silently dropping the data", () => {
            expect(result).toEqual({ error: expect.any(Error) });
        });
    });

    describe("Scenario: a binary response comes back with no confidence field on the wire", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "binary", trueProbability: 0.97 } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({ question: { kind: "binary", instructions: "Is this spam?" } });
        });

        it("maps the wire's trueProbability onto the result", () => {
            expect(result.data).toMatchObject({ trueProbability: 0.97 });
        });

        it("has no answer field", () => {
            expect(result.data).not.toHaveProperty("answer");
        });
    });

    describe("Scenario: a choice response comes back with no confidence field on the wire", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "choice", choice: "a", probabilities: { a: 1 } } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({
                question: { kind: "choice", instructions: "Pick one", options: [{ key: "a" }] },
            });
        });

        it("omits confidence entirely rather than sending it as undefined", () => {
            expect(result.data).not.toHaveProperty("confidence");
        });
    });

    describe("Scenario: a choice response comes back with a confidence field on the wire", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "choice", choice: "a", confidence: 0.6, probabilities: { a: 1 } } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({
                question: { kind: "choice", instructions: "Pick one", options: [{ key: "a" }] },
            });
        });

        it("maps the wire's confidence onto the result", () => {
            expect(result.data).toMatchObject({ confidence: 0.6 });
        });
    });

    describe("Scenario: a score response comes back with no confidence field on the wire", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({
                data: { $type: "score", score: 1.8, probabilities: { "0": 0.05, "1": 0.15, "2": 0.8 } },
            });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({
                question: {
                    kind: "score",
                    instructions: "How good?",
                    levels: [{ description: "poor" }, { description: "ok" }, { description: "good" }],
                },
            });
        });

        it("maps the wire's score onto the result", () => {
            expect(result.data).toMatchObject({ score: 1.8 });
        });

        it("keys probabilities by level index", () => {
            expect((result.data as { probabilities?: Record<number, number> } | undefined)?.probabilities?.[2]).toBe(
                0.8,
            );
        });

        it("has no level field", () => {
            expect(result.data).not.toHaveProperty("level");
        });

        it("omits confidence entirely rather than sending it as undefined", () => {
            expect(result.data).not.toHaveProperty("confidence");
        });
    });

    describe("Scenario: a score response comes back with a confidence field on the wire", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({
                data: { $type: "score", score: 1.8, confidence: 0.8, probabilities: { "0": 0.05, "1": 0.15, "2": 0.8 } },
            });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({
                question: {
                    kind: "score",
                    instructions: "How good?",
                    levels: [{ description: "poor" }, { description: "ok" }, { description: "good" }],
                },
            });
        });

        it("maps the wire's confidence onto the result", () => {
            expect(result.data).toMatchObject({ confidence: 0.8 });
        });
    });

    describe("Scenario: a score response's probabilities include a non-numeric key", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({
                data: { $type: "score", score: 0.5, probabilities: { "0": 0.5, x: 0.5 } },
            });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({
                question: { kind: "score", instructions: "How good?", levels: [{ description: "poor" }] },
            });
        });

        it("drops the non-numeric key, keeping only the valid level index", () => {
            expect(Object.keys((result.data as { probabilities?: Record<number, number> } | undefined)?.probabilities ?? {})).toEqual(["0"]);
        });
    });
});
