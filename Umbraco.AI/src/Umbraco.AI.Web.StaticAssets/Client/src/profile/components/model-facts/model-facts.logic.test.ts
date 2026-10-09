// MF-6 — Admins see the selected model's facts under the Model field
import { describe, expect, it } from "vitest";

import { isSafeFactUrl, selectDisplayFacts } from "./model-facts.logic.js";
import type { UaiModelFactModel, UaiModelFactTone } from "../../../connection/types.js";

/**
 * Pure display rules for `<uai-model-facts>` (PLAN T9): warnings first, at most six facts, and only
 * absolute http/https links. Kept apart from the element so the rules are cheap to pin down.
 */

type Fact = UaiModelFactModel;
type Tone = UaiModelFactTone;

const fact = (key: string, tone: Tone = "Neutral", url?: string | null): Fact => ({
    key,
    label: `#uaiModelFacts_${key}`,
    value: key,
    tone,
    url: url ?? undefined,
});

describe("Feature: model facts display logic", () => {
    describe("Happy path", () => {
        describe("Scenario: the model has a Neutral fact and a Warning fact", () => {
            const facts = [fact("neutral"), fact("warning", "Warning")];

            it("AC3: puts the Warning fact first", () => {
                expect(selectDisplayFacts(facts)[0].key).toBe("warning");
            });

            it("AC3: keeps the Neutral fact after the Warning fact", () => {
                expect(selectDisplayFacts(facts)[1].key).toBe("neutral");
            });
        });

        describe("Scenario: several facts share a tone", () => {
            const facts = [fact("a"), fact("w1", "Warning"), fact("b", "Positive"), fact("w2", "Warning")];

            it("AC3/SPEC: keeps the server's order within warnings and within the rest", () => {
                expect(selectDisplayFacts(facts).map((f) => f.key)).toEqual(["w1", "w2", "a", "b"]);
            });
        });

        describe("Scenario: the model has 8 facts", () => {
            const facts = ["f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8"].map((k) => fact(k));

            it("AC6: keeps 6 facts", () => {
                expect(selectDisplayFacts(facts)).toHaveLength(6);
            });

            it("AC6: keeps the first 6 in the order the server sent them", () => {
                expect(selectDisplayFacts(facts).map((f) => f.key)).toEqual(["f1", "f2", "f3", "f4", "f5", "f6"]);
            });
        });

        describe("Scenario: 8 facts where the last one is a Warning", () => {
            const facts = [...["f1", "f2", "f3", "f4", "f5", "f6", "f7"].map((k) => fact(k)), fact("w", "Warning")];

            it("AC3/AC6: a Warning past the cap still makes the cut, first", () => {
                expect(selectDisplayFacts(facts)[0].key).toBe("w");
            });
        });

        describe("Scenario: a fact has an https Url", () => {
            it("AC5: treats https://example.com/model as safe", () => {
                expect(isSafeFactUrl("https://example.com/model")).toBe(true);
            });

            it("AC5/SPEC: treats an absolute http URL as safe", () => {
                expect(isSafeFactUrl("http://example.com/model")).toBe(true);
            });
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the model has no facts", () => {
            it("AC9: returns no facts to render", () => {
                expect(selectDisplayFacts([])).toEqual([]);
            });
        });

        describe("Scenario: a fact has an unsafe or missing Url", () => {
            it("AC5: rejects a javascript: URL", () => {
                expect(isSafeFactUrl("javascript:alert(1)")).toBe(false);
            });

            it("AC5: rejects a data: URL", () => {
                expect(isSafeFactUrl("data:text/html,<b>x</b>")).toBe(false);
            });

            it("AC5: rejects a relative URL", () => {
                expect(isSafeFactUrl("/model")).toBe(false);
            });

            it("AC5: rejects a malformed URL", () => {
                expect(isSafeFactUrl("https//example")).toBe(false);
            });

            it("AC5: rejects a null URL", () => {
                expect(isSafeFactUrl(null)).toBe(false);
            });

            it("AC5: rejects an empty URL", () => {
                expect(isSafeFactUrl("")).toBe(false);
            });
        });
    });
});
