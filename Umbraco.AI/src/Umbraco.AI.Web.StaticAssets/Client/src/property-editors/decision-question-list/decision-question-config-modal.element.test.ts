// DR-16 — Ask several questions in one Automate step (AC7): the config modal refuses a bad alias,
// and only ever submits the exact, validated flat question shape.
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import "./decision-question-config-modal.element.js";
import type { UaiDecisionQuestionConfigModalElement } from "./decision-question-config-modal.element.js";
import type { UaiDecisionQuestionKind, UaiDecisionQuestionListItem } from "./types.js";

beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), form: null } as unknown as ElementInternals;
        };
    }
});

interface RenderOptions {
    kind: UaiDecisionQuestionKind;
    alias?: string;
    instructions?: string;
    otherAliases?: string[];
    trueCriteria?: string;
    falseCriteria?: string;
    /** Raw input value; omit to leave the field's own default (0.5) untouched. */
    threshold?: string;
    options?: Array<{ key: string; value?: string }>;
    levels?: string[];
}

async function renderModal(opts: RenderOptions) {
    const el = document.createElement("uai-decision-question-config-modal") as UaiDecisionQuestionConfigModalElement;
    const submit = vi.fn();
    const setValue = vi.fn();
    el.data = { kind: opts.kind, otherAliases: opts.otherAliases ?? [] } as never;
    el.modalContext = { submit, reject: vi.fn(), setValue, getValue: vi.fn() } as never;
    document.body.appendChild(el);
    await el.updateComplete;

    const set = (id: string, value: string) => {
        const input = el.shadowRoot!.getElementById(id) as HTMLElement & { value?: string };
        input.value = value;
        input.dispatchEvent(new Event("input"));
    };

    set("alias", opts.alias ?? "refund_1");
    set("instructions", opts.instructions ?? "Is it?");

    if (opts.kind === "binary") {
        if (opts.trueCriteria !== undefined) set("true-criteria", opts.trueCriteria);
        if (opts.falseCriteria !== undefined) set("false-criteria", opts.falseCriteria);
        if (opts.threshold !== undefined) set("threshold", opts.threshold);
    } else if (opts.kind === "choice" && opts.options) {
        const optionsEl = el.shadowRoot!.getElementById("options") as HTMLElement & { value?: unknown };
        optionsEl.value = opts.options;
        optionsEl.dispatchEvent(new Event("change"));
    } else if (opts.kind === "score" && opts.levels) {
        const levelsEl = el.shadowRoot!.getElementById("levels") as HTMLElement & { items?: unknown };
        levelsEl.items = opts.levels;
        levelsEl.dispatchEvent(new Event("change"));
    }

    await el.updateComplete;
    el.shadowRoot!.getElementById("btn-submit")!.dispatchEvent(new MouseEvent("click"));
    await el.updateComplete;
    return { el, submit, setValue };
}

/** The `question` from the (only) `setValue` call, or `undefined` if it was never called. */
function submittedQuestion(setValue: ReturnType<typeof vi.fn>): UaiDecisionQuestionListItem | undefined {
    return (setValue.mock.calls[0]?.[0] as { question?: UaiDecisionQuestionListItem } | undefined)?.question;
}

describe("Feature: decision question config modal", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: a valid alias", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({ kind: "binary", alias: "refund_1" }));
        });

        it("submits", () => {
            expect(submit).toHaveBeenCalledOnce();
        });
    });

    describe("Scenario: an invalid alias", () => {
        for (const [why, alias, others] of [
            ["is blank", "", []],
            ["starts with a digit", "1refund", []],
            ["has a space", "re fund", []],
            ["duplicates another question's", "refund", ["refund"]],
        ] as const) {
            describe(`when it ${why}`, () => {
                let el: UaiDecisionQuestionConfigModalElement;
                let submit: ReturnType<typeof vi.fn>;

                beforeEach(async () => {
                    ({ el, submit } = await renderModal({ kind: "binary", alias, otherAliases: [...others] }));
                });

                it("doesn't submit", () => {
                    expect(submit).not.toHaveBeenCalled();
                });

                it("shows why", () => {
                    expect(el.shadowRoot!.querySelector(".alias-error")).not.toBeNull();
                });
            });
        }
    });

    describe("Scenario: a valid yes/no question", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ setValue } = await renderModal({
                kind: "binary",
                alias: "refund",
                instructions: "Does the customer want a refund?",
                trueCriteria: "Explicitly asks for money back",
                falseCriteria: "Just venting",
                threshold: "0.7",
            }));
        });

        it("submits the exact flat question", () => {
            expect(submittedQuestion(setValue)).toEqual({
                kind: "binary",
                alias: "refund",
                instructions: "Does the customer want a refund?",
                trueCriteria: "Explicitly asks for money back",
                falseCriteria: "Just venting",
                threshold: 0.7,
            });
        });
    });

    describe("Scenario: a valid pick-one question", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ setValue } = await renderModal({
                kind: "choice",
                alias: "category",
                instructions: "What is this about?",
                options: [
                    { key: "billing", value: "Billing question" },
                    { key: "technical", value: "Technical issue" },
                ],
            }));
        });

        it("submits the exact flat question, with no binary fields", () => {
            expect(submittedQuestion(setValue)).toEqual({
                kind: "choice",
                alias: "category",
                instructions: "What is this about?",
                options: [
                    { key: "billing", value: "Billing question" },
                    { key: "technical", value: "Technical issue" },
                ],
            });
        });
    });

    describe("Scenario: a valid score question", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ setValue } = await renderModal({
                kind: "score",
                alias: "mood",
                instructions: "How happy does the customer sound?",
                levels: ["Unhappy", "Neutral", "Happy"],
            }));
        });

        it("submits the exact flat question", () => {
            expect(submittedQuestion(setValue)).toEqual({
                kind: "score",
                alias: "mood",
                instructions: "How happy does the customer sound?",
                levels: ["Unhappy", "Neutral", "Happy"],
            });
        });
    });

    describe("Scenario: submitted option keys and levels are trimmed", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ setValue } = await renderModal({
                kind: "choice",
                options: [
                    { key: " billing ", value: "Billing question" },
                    { key: "technical", value: "Technical issue" },
                ],
            }));
        });

        it("trims the stored option keys", () => {
            expect(submittedQuestion(setValue)?.options?.map((o) => o.key)).toEqual(["billing", "technical"]);
        });
    });

    describe("Scenario: blank instructions", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({ kind: "binary", instructions: "" }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: a blank threshold", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            // Number("") is 0, which is in range — this pins that a blank input is refused rather
            // than silently saved as a threshold of 0 ("always yes").
            ({ submit } = await renderModal({ kind: "binary", threshold: "" }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: a threshold out of range", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({ kind: "binary", threshold: "1.5" }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: fewer than 2 options", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({ kind: "choice", options: [{ key: "billing" }] }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: duplicate option keys", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({
                kind: "choice",
                options: [{ key: "billing" }, { key: "billing" }],
            }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: fewer than 2 levels", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            ({ submit } = await renderModal({ kind: "score", levels: ["Unhappy"] }));
        });

        it("doesn't submit", () => {
            expect(submit).not.toHaveBeenCalled();
        });
    });
});
