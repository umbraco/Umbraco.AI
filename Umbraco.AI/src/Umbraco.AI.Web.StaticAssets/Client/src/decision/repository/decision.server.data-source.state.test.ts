// DR-5 — Ask decisions from TypeScript (AC5): state goes on the request body, not the question.
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the generated OpenAPI SDK call.
const sdkAsk = vi.fn();
vi.mock("../../api/sdk.gen.js", () => ({
    DecisionService: { ask: (...args: unknown[]) => sdkAsk(...args) },
}));
vi.mock("@umbraco-cms/backoffice/resources", () => ({
    tryExecute: async (_host: unknown, promise: Promise<unknown>) => promise,
}));

import { UaiDecisionServerDataSource } from "./decision.server.data-source.js";

const TEST_HOST_TAG = "uai-decision-data-source-state-test-host";
if (!customElements.get(TEST_HOST_TAG)) {
    customElements.define(TEST_HOST_TAG, class extends UmbControllerHostElementMixin(HTMLElement) {});
}

function createHost(): UmbControllerHost {
    const element = document.createElement(TEST_HOST_TAG);
    document.body.appendChild(element);
    return element as unknown as UmbControllerHost;
}

describe("Feature: decision server data source state", () => {
    describe("Scenario: a question is sent with state and a profile", () => {
        beforeEach(async () => {
            sdkAsk.mockReset();
            sdkAsk.mockResolvedValue({ data: { $type: "binary", trueProbability: 0.9 } });
            await new UaiDecisionServerDataSource(createHost()).ask({
                question: { kind: "binary", instructions: "Is this spam?" },
                state: "Buy cheap watches",
                profileIdOrAlias: "spam-check",
            });
        });

        it("sends the state at the top level of the body", () => {
            expect(sdkAsk.mock.calls[0][0].body.state).toBe("Buy cheap watches");
        });

        it("doesn't send a context on the question", () => {
            expect(sdkAsk.mock.calls[0][0].body.question).not.toHaveProperty("context");
        });

        it("still forwards the profile id or alias", () => {
            expect(sdkAsk.mock.calls[0][0].body.profileIdOrAlias).toBe("spam-check");
        });
    });
});
