// DR-5 — Ask decisions from TypeScript (AC4, AC5)
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

        // Pending T14
        it.skip("sends the question kind as the API's $type", () => {
            expect(sdkAsk.mock.calls[0][0].body.question.$type).toBe("choice");
        });

        // Pending T14
        it.skip("forwards the profile id or alias", () => {
            expect(sdkAsk.mock.calls[0][0].body.profileIdOrAlias).toBe("spam-check");
        });
    });

    describe("Scenario: a response comes back from the API", () => {
        let result: Awaited<ReturnType<UaiDecisionServerDataSource["ask"]>>;

        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "binary", answer: false, probability: 0.2, confidence: 0.8 } });
            const dataSource = new UaiDecisionServerDataSource(createHost());
            result = await dataSource.ask({ question: { kind: "binary", instructions: "Is this spam?" } });
        });

        // Pending T14
        it.skip("maps the API's $type back to kind", () => {
            expect(result.data?.kind).toBe("binary");
        });
    });
});
