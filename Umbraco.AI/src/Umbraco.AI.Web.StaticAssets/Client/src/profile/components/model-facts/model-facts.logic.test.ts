// MF-6 — Admins see the selected model's facts under the Model field
import { describe, expect, it } from "vitest";

/**
 * Pure display rules for `<uai-model-facts>` (PLAN T9): warnings first, at most six facts, and only
 * absolute http/https links. Kept apart from the element so the rules are cheap to pin down.
 *
 * PENDING: the module does not exist yet. It is loaded through a non-literal dynamic import inside
 * each (skipped) test body, so neither `tsc` (build:api / build:app type-check `src/**`) nor Vitest's
 * collection step tries to resolve it. When T9 lands, swap `loadLogic()` for a static import and
 * turn each `it.skip` into `it`.
 */

type Tone = "Neutral" | "Positive" | "Warning";

interface Fact {
    key: string;
    label: string;
    value: string;
    tone: Tone;
    url?: string | null;
}

interface ModelFactsLogic {
    /** Facts to render, in render order: Warning tone first, otherwise server order, capped at 6. */
    selectDisplayFacts(facts: Fact[]): Fact[];
    /** True only for an absolute http/https URL. */
    isSafeFactUrl(url: string | null | undefined): boolean;
}

const LOGIC_MODULE = "./model-facts.logic.js";

async function loadLogic(): Promise<ModelFactsLogic> {
    return (await import(/* @vite-ignore */ LOGIC_MODULE)) as ModelFactsLogic;
}

const fact = (key: string, tone: Tone = "Neutral", url?: string | null): Fact => ({
    key,
    label: `#uaiModelFacts_${key}`,
    value: key,
    tone,
    url,
});

describe("Feature: model facts display logic", () => {
    describe("Happy path", () => {
        describe("Scenario: the model has a Neutral fact and a Warning fact", () => {
            const facts = [fact("neutral"), fact("warning", "Warning")];

            it.skip("[pending T9] AC3: puts the Warning fact first", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts)[0].key).toBe("warning");
            });

            it.skip("[pending T9] AC3: keeps the Neutral fact after the Warning fact", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts)[1].key).toBe("neutral");
            });
        });

        describe("Scenario: several facts share a tone", () => {
            const facts = [fact("a"), fact("w1", "Warning"), fact("b", "Positive"), fact("w2", "Warning")];

            it.skip("[pending T9] AC3/SPEC: keeps the server's order within warnings and within the rest", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts).map((f) => f.key)).toEqual(["w1", "w2", "a", "b"]);
            });
        });

        describe("Scenario: the model has 8 facts", () => {
            const facts = ["f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8"].map((k) => fact(k));

            it.skip("[pending T9] AC6: keeps 6 facts", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts)).toHaveLength(6);
            });

            it.skip("[pending T9] AC6: keeps the first 6 in the order the server sent them", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts).map((f) => f.key)).toEqual(["f1", "f2", "f3", "f4", "f5", "f6"]);
            });
        });

        describe("Scenario: 8 facts where the last one is a Warning", () => {
            const facts = [...["f1", "f2", "f3", "f4", "f5", "f6", "f7"].map((k) => fact(k)), fact("w", "Warning")];

            it.skip("[pending T9] AC3/AC6: a Warning past the cap still makes the cut, first", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts(facts)[0].key).toBe("w");
            });
        });

        describe("Scenario: a fact has an https Url", () => {
            it.skip("[pending T9] AC5: treats https://example.com/model as safe", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("https://example.com/model")).toBe(true);
            });

            it.skip("[pending T9] AC5/SPEC: treats an absolute http URL as safe", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("http://example.com/model")).toBe(true);
            });
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the model has no facts", () => {
            it.skip("[pending T9] AC9: returns no facts to render", async () => {
                const { selectDisplayFacts } = await loadLogic();
                expect(selectDisplayFacts([])).toEqual([]);
            });
        });

        describe("Scenario: a fact has an unsafe or missing Url", () => {
            it.skip("[pending T9] AC5: rejects a javascript: URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("javascript:alert(1)")).toBe(false);
            });

            it.skip("[pending T9] AC5: rejects a data: URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("data:text/html,<b>x</b>")).toBe(false);
            });

            it.skip("[pending T9] AC5: rejects a relative URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("/model")).toBe(false);
            });

            it.skip("[pending T9] AC5: rejects a malformed URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("https//example")).toBe(false);
            });

            it.skip("[pending T9] AC5: rejects a null URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl(null)).toBe(false);
            });

            it.skip("[pending T9] AC5: rejects an empty URL", async () => {
                const { isSafeFactUrl } = await loadLogic();
                expect(isSafeFactUrl("")).toBe(false);
            });
        });
    });
});
