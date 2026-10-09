// MF-6 — Admins see the selected model's facts under the Model field
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UaiConnectionModelFactsRepository } from "../../../connection/repository/model-facts/connection-model-facts.repository.js";
import type { UaiModelFactModel, UaiModelFactTone, UaiModelFactsModel } from "../../../connection/types.js";
import type { UaiConnectionModelFactsRequestArgs } from "../../../connection/repository/model-facts/connection-model-facts.server.data-source.js";
import "./model-facts.element.js"; // registers <uai-model-facts>
import type { UaiModelFactsElement } from "./model-facts.element.js";

/**
 * `<uai-model-facts>` driven only through its public properties (`connectionId`, `capability`,
 * `modelId`), rendered for real into happy-dom. The one collaborator stubbed is the facts repository
 * (PLAN T8), at its prototype, so the element's own wiring stays real.
 *
 * Markup hooks the element exposes are kept in `SELECTORS`; if they change, change them there, not
 * in each test.
 *
 * Left to the T10 live (Playwright) check: AC1's English label text and AC4's tooltip on
 * hover/focus, which both need the real localization registry and real UUI popover behaviour.
 */

const SELECTORS = {
    area: "[data-model-facts]",
    row: "[data-fact-key]",
    warningStyle: 'uui-tag[color="warning"]',
    link: "a[href]",
    loader: "uui-loader-bar",
} as const;

type Tone = UaiModelFactTone;
type Fact = UaiModelFactModel;
type RequestArgs = UaiConnectionModelFactsRequestArgs;

interface RequestResult {
    data?: UaiModelFactsModel[];
    error?: unknown;
}

type ModelFactsElement = UaiModelFactsElement;

const fact = (key: string, value: string, tone: Tone = "Neutral", url?: string): Fact => ({
    key,
    label: `#uaiModelFacts_${key}`,
    value,
    tone,
    url,
});

const contextWindow = (value = "200,000") => fact("core.contextWindow", value);

const factsFor = (modelId: string, facts: Fact[]): RequestResult => ({
    data: [{ model: { providerId: "openrouter", modelId }, facts }],
});

function deferred<T>() {
    let resolve!: (value: T) => void;
    const promise = new Promise<T>((r) => (resolve = r));
    return { promise, resolve };
}

function stubRepository(impl: (args: RequestArgs) => Promise<RequestResult>) {
    return vi.spyOn(UaiConnectionModelFactsRepository.prototype, "requestModelFacts").mockImplementation(impl);
}

/** Lets pending promises resolve and Lit re-render. */
async function settle(el: ModelFactsElement) {
    for (let i = 0; i < 3; i++) {
        await new Promise((r) => setTimeout(r, 0));
        await el.updateComplete;
    }
}

async function render(props: Partial<RequestArgs>): Promise<ModelFactsElement> {
    const el = document.createElement("uai-model-facts");
    Object.assign(el, props);
    document.body.appendChild(el);
    await settle(el);
    return el;
}

const query = (el: ModelFactsElement, selector: string) => el.shadowRoot!.querySelector(selector);
const queryAll = (el: ModelFactsElement, selector: string) => el.shadowRoot!.querySelectorAll(selector);
const text = (el: ModelFactsElement) => el.shadowRoot!.textContent ?? "";

const selected = { connectionId: "conn-1", capability: "Chat", modelId: "m1" };

afterEach(() => {
    vi.restoreAllMocks();
    document.body.innerHTML = "";
});

describe("Feature: model facts under the Model field", () => {
    describe("Happy path", () => {
        describe("Scenario: a model with a context window is selected", () => {
            let el: ModelFactsElement;
            let request: ReturnType<typeof stubRepository>;

            beforeEach(async () => {
                request = stubRepository(async () => factsFor("m1", [contextWindow()]));
                el = await render(selected);
            });

            it("AC1: requests facts for the selected connection, capability and model", () => {
                expect(request).toHaveBeenCalledWith(selected);
            });

            it("AC1: renders the context window row", () => {
                expect(query(el, '[data-fact-key="core.contextWindow"]')).not.toBeNull();
            });

            it("AC1: shows the context window value", () => {
                expect(text(el)).toContain("200,000");
            });
        });

        describe("Scenario: model m1's facts are shown and the admin selects m2", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async ({ modelId }) =>
                    factsFor(modelId!, [contextWindow(modelId === "m1" ? "111,111" : "222,222")]),
                );
                el = await render(selected);
                el.modelId = "m2";
                await settle(el);
            });

            it("AC2: shows m2's facts", () => {
                expect(text(el)).toContain("222,222");
            });

            it("AC2: no longer shows m1's facts", () => {
                expect(text(el)).not.toContain("111,111");
            });
        });

        describe("Scenario: the selected model has a Neutral fact and a Warning fact", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async () =>
                    factsFor("m1", [fact("core.neutral", "n"), fact("core.warning", "w", "Warning")]),
                );
                el = await render(selected);
            });

            it("AC3: lists the Warning fact first", () => {
                expect(queryAll(el, SELECTORS.row)[0].getAttribute("data-fact-key")).toBe("core.warning");
            });

            it("AC3: styles the Warning fact as a warning", () => {
                expect(queryAll(el, SELECTORS.row)[0].querySelector(SELECTORS.warningStyle)).not.toBeNull();
            });

            it("AC3: does not style the Neutral fact as a warning", () => {
                expect(queryAll(el, SELECTORS.row)[1].querySelector(SELECTORS.warningStyle)).toBeNull();
            });
        });

        describe("Scenario: a fact has the Url https://example.com/model", () => {
            let link: Element | null;

            beforeEach(async () => {
                stubRepository(async () =>
                    factsFor("m1", [fact("core.price", "$3", "Neutral", "https://example.com/model")]),
                );
                const el = await render(selected);
                link = query(el, SELECTORS.link);
            });

            it("AC5: links to the Url", () => {
                expect(link?.getAttribute("href")).toBe("https://example.com/model");
            });

            it("AC5: opens the link in a new tab", () => {
                expect(link?.getAttribute("target")).toBe("_blank");
            });

            it('AC5: sets rel="noopener noreferrer"', () => {
                expect(link?.getAttribute("rel")).toBe("noopener noreferrer");
            });
        });

        describe("Scenario: the selected model has 8 facts", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async () =>
                    factsFor(
                        "m1",
                        ["f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8"].map((k) => fact(`core.${k}`, k)),
                    ),
                );
                el = await render(selected);
            });

            it("AC6: shows 6 rows", () => {
                expect(queryAll(el, SELECTORS.row)).toHaveLength(6);
            });
        });

        describe("Scenario: the facts request is in flight", () => {
            let el: ModelFactsElement;
            let pending: ReturnType<typeof deferred<RequestResult>>;

            beforeEach(async () => {
                pending = deferred<RequestResult>();
                stubRepository(() => pending.promise);
                el = await render(selected);
            });

            it("AC7: shows a loader in the facts area", () => {
                expect(query(el, `${SELECTORS.area} ${SELECTORS.loader}`)).not.toBeNull();
            });

            it("AC7: removes the loader once the facts arrive", async () => {
                pending.resolve(factsFor("m1", [contextWindow()]));
                await settle(el);
                expect(query(el, SELECTORS.loader)).toBeNull();
            });
        });
    });

    describe("Sad path", () => {
        describe("Scenario: a connection is chosen but no model", () => {
            let el: ModelFactsElement;
            let request: ReturnType<typeof stubRepository>;

            beforeEach(async () => {
                request = stubRepository(async () => factsFor("m1", [contextWindow()]));
                el = await render({ connectionId: "conn-1", capability: "Chat" });
            });

            it("AC8: renders no facts area", () => {
                expect(query(el, SELECTORS.area)).toBeNull();
            });

            it("AC8: makes no facts request", () => {
                expect(request).not.toHaveBeenCalled();
            });
        });

        describe("Scenario: the selected model has no facts", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async () => ({ data: [] }));
                el = await render(selected);
            });

            it("AC9: renders no facts area", () => {
                expect(query(el, SELECTORS.area)).toBeNull();
            });
        });

        describe("Scenario: the facts request fails", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                vi.spyOn(console, "error").mockImplementation(() => {});
                vi.spyOn(console, "warn").mockImplementation(() => {});
                stubRepository(async () => ({ error: new Error("boom") }));
                el = await render(selected);
            });

            it("AC10: renders no facts area", () => {
                expect(query(el, SELECTORS.area)).toBeNull();
            });

            it("AC10: logs a warning to the console", () => {
                expect(console.warn).toHaveBeenCalledTimes(1);
            });

            it("AC10: shows no error message", () => {
                expect(text(el).trim()).toBe("");
            });
        });

        describe("Scenario: m1's response arrives after m2's", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                const m1 = deferred<RequestResult>();
                stubRepository(({ modelId }) =>
                    modelId === "m1" ? m1.promise : Promise.resolve(factsFor("m2", [contextWindow("222,222")])),
                );
                el = await render(selected);
                el.modelId = "m2";
                await settle(el);
                m1.resolve(factsFor("m1", [contextWindow("111,111")]));
                await settle(el);
            });

            it("AC11: shows m2's facts", () => {
                expect(text(el)).toContain("222,222");
            });

            it("AC11: ignores m1's late response", () => {
                expect(text(el)).not.toContain("111,111");
            });
        });

        describe("Scenario: facts are shown and the admin changes the connection", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async () => factsFor("m1", [contextWindow()]));
                el = await render(selected);
                // The Settings view clears the model when the connection changes (SPEC, Profile Settings view change).
                el.connectionId = "conn-2";
                el.modelId = undefined;
                await settle(el);
            });

            it("AC12: renders no facts area", () => {
                expect(query(el, SELECTORS.area)).toBeNull();
            });
        });

        describe("Scenario: a fact has a javascript: Url", () => {
            let el: ModelFactsElement;

            beforeEach(async () => {
                stubRepository(async () =>
                    factsFor("m1", [fact("core.price", "$3", "Neutral", "javascript:alert(1)")]),
                );
                el = await render(selected);
            });

            it("AC5: renders no link", () => {
                expect(query(el, SELECTORS.link)).toBeNull();
            });
        });
    });
});
