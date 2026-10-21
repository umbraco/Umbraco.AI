// EX-9 — Set up fields on an Extract Values step (AC3-AC9, AC12-AC16): the field config modal.
//
// Copies decision-question-config-modal.element.test.ts. Assumed names (rename here if the build
// differs):
// - element `uai-extraction-field-config-modal` (UaiExtractionFieldConfigModalElement)
// - data { kind, existingField?, otherAliases }; submits setValue({ field }) then submit()
// - inputs by id: alias, instructions, decimals, integer (uui-toggle), unit, item-type
// - buttons: btn-submit (Save), btn-cancel (Cancel)
// - errors render under their field as `.alias-error` / `.instructions-error`
// - each field sits in an `umb-property-layout` whose `label` is the English default (termOrDefault)
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import "./extraction-field-config-modal.element.js";
import type { UaiExtractionFieldConfigModalElement } from "./extraction-field-config-modal.element.js";
import type { UaiExtractionFieldKind, UaiExtractionFieldListItem } from "./types.js";

beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), form: null } as unknown as ElementInternals;
        };
    }
});

interface OpenOptions {
    kind: UaiExtractionFieldKind;
    existingField?: UaiExtractionFieldListItem;
    otherAliases?: string[];
}

async function openModal(opts: OpenOptions) {
    const el = document.createElement("uai-extraction-field-config-modal") as UaiExtractionFieldConfigModalElement;
    const submit = vi.fn();
    const reject = vi.fn();
    const setValue = vi.fn();
    el.data = { kind: opts.kind, existingField: opts.existingField, otherAliases: opts.otherAliases ?? [] } as never;
    el.modalContext = { submit, reject, setValue, getValue: vi.fn() } as never;
    document.body.appendChild(el);
    await el.updateComplete;
    return { el, submit, reject, setValue };
}

function input(el: HTMLElement, id: string) {
    return el.shadowRoot!.getElementById(id) as (HTMLElement & { value?: unknown; checked?: boolean }) | null;
}

function setInput(el: HTMLElement, id: string, value: string) {
    const target = input(el, id)!;
    target.value = value;
    target.dispatchEvent(new Event("input"));
}

function setToggle(el: HTMLElement, id: string, checked: boolean) {
    const target = input(el, id)!;
    target.checked = checked;
    target.dispatchEvent(new Event("change"));
}

async function clickButton(el: UaiExtractionFieldConfigModalElement, id: string) {
    el.shadowRoot!.getElementById(id)!.dispatchEvent(new MouseEvent("click"));
    await el.updateComplete;
}

async function fillAndSave(
    el: UaiExtractionFieldConfigModalElement,
    values: { alias?: string; instructions?: string },
) {
    setInput(el, "alias", values.alias ?? "orderId");
    setInput(el, "instructions", values.instructions ?? "The order number");
    await el.updateComplete;
    await clickButton(el, "btn-submit");
}

const headline = (el: HTMLElement) => el.shadowRoot!.querySelector("umb-body-layout")?.getAttribute("headline");
const fieldLabels = (el: HTMLElement) =>
    Array.from(el.shadowRoot!.querySelectorAll("umb-property-layout")).map((l) => l.getAttribute("label"));

/** The `field` from the (only) `setValue` call, or `undefined` if it was never called. */
function submittedField(setValue: ReturnType<typeof vi.fn>): UaiExtractionFieldListItem | undefined {
    return (setValue.mock.calls[0]?.[0] as { field?: UaiExtractionFieldListItem } | undefined)?.field;
}

describe("Feature: extraction field config modal", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: a Number field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "number" }));
        });

        it.skip('shows the headline "Add field: Number"', () => {
            expect(headline(el)).toBe("Add field: Number");
        });

        it.skip("shows Alias, Instructions, Decimal places, Whole number and Unit", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Decimal places", "Whole number", "Unit"]);
        });
    });

    describe("Scenario: a Duration field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "duration" }));
        });

        it.skip("shows Alias, Instructions, Unit, Decimal places and Whole number", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Unit", "Decimal places", "Whole number"]);
        });

        it.skip("defaults the unit to seconds", () => {
            expect(input(el, "unit")?.value).toBe("seconds");
        });
    });

    describe("Scenario: a List field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "list" }));
        });

        it.skip("shows Alias, Instructions and Item type", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Item type"]);
        });

        it.skip("defaults the item type to Text", () => {
            expect(input(el, "item-type")?.value).toBe("text");
        });
    });

    describe("Scenario: a field type with no extra settings is added", () => {
        for (const [kind, title] of [
            ["text", "Text"],
            ["date", "Date"],
            ["time", "Time"],
            ["datetime", "Date and time"],
        ] as const) {
            describe(`when it is ${title}`, () => {
                let el: UaiExtractionFieldConfigModalElement;

                beforeEach(async () => {
                    ({ el } = await openModal({ kind }));
                });

                it.skip("shows only Alias and Instructions", () => {
                    expect(fieldLabels(el)).toEqual(["Alias", "Instructions"]);
                });

                it.skip(`shows the headline "Add field: ${title}"`, () => {
                    expect(headline(el)).toBe(`Add field: ${title}`);
                });
            });
        }
    });

    describe("Scenario: a filled-in Number field is saved", () => {
        let submit: ReturnType<typeof vi.fn>;
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, submit, setValue } = await openModal({ kind: "number", otherAliases: ["orderId"] }));
            setInput(el, "decimals", "2");
            setToggle(el, "integer", true);
            setInput(el, "unit", "DKK");
            await fillAndSave(el, { alias: "amount", instructions: "Amount paid" });
        });

        it.skip("submits", () => {
            expect(submit).toHaveBeenCalledOnce();
        });

        it.skip("submits the exact flat field", () => {
            expect(submittedField(setValue)).toEqual({
                kind: "number",
                alias: "amount",
                instructions: "Amount paid",
                decimals: 2,
                integer: true,
                unit: "DKK",
            });
        });
    });

    describe("Scenario: a Text field is saved", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, setValue } = await openModal({ kind: "text" }));
            await fillAndSave(el, { alias: "orderId", instructions: "The order number" });
        });

        it.skip("submits only kind, alias and instructions", () => {
            expect(submittedField(setValue)).toEqual({ kind: "text", alias: "orderId", instructions: "The order number" });
        });
    });

    describe("Scenario: a saved Number field is edited", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({
                kind: "number",
                existingField: { kind: "number", alias: "amount", instructions: "Amount paid", decimals: 2, unit: "DKK" },
                otherAliases: ["orderId"],
            }));
        });

        it.skip('shows the headline "Edit field: Number"', () => {
            expect(headline(el)).toBe("Edit field: Number");
        });

        it.skip("fills in the saved alias", () => {
            expect(input(el, "alias")?.value).toBe("amount");
        });

        it.skip("fills in the saved instructions", () => {
            expect(input(el, "instructions")?.value).toBe("Amount paid");
        });

        it.skip("fills in the saved decimal places", () => {
            expect(String(input(el, "decimals")?.value)).toBe("2");
        });

        it.skip("fills in the saved unit", () => {
            expect(input(el, "unit")?.value).toBe("DKK");
        });
    });

    describe("Scenario: a saved field keeps its own alias when edited", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, submit } = await openModal({
                kind: "text",
                existingField: { kind: "text", alias: "orderId", instructions: "The order number" },
                otherAliases: ["amount"],
            }));
            await fillAndSave(el, { alias: "orderId", instructions: "The order reference" });
        });

        it.skip("submits", () => {
            expect(submit).toHaveBeenCalledOnce();
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the alias is empty", () => {
            let el: UaiExtractionFieldConfigModalElement;
            let submit: ReturnType<typeof vi.fn>;

            beforeEach(async () => {
                ({ el, submit } = await openModal({ kind: "text" }));
                await fillAndSave(el, { alias: "" });
            });

            it.skip("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it.skip("shows a message under Alias", () => {
                expect(el.shadowRoot!.querySelector(".alias-error")).not.toBeNull();
            });
        });

        describe("Scenario: the instructions are empty", () => {
            let el: UaiExtractionFieldConfigModalElement;
            let submit: ReturnType<typeof vi.fn>;

            beforeEach(async () => {
                ({ el, submit } = await openModal({ kind: "text" }));
                await fillAndSave(el, { instructions: "   " });
            });

            it.skip("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it.skip("shows a message under Instructions", () => {
                expect(el.shadowRoot!.querySelector(".instructions-error")).not.toBeNull();
            });
        });

        describe("Scenario: the alias has the wrong format", () => {
            for (const [why, alias] of [
                ["starts with a digit and has a hyphen", "1st-value"],
                ["has a space", "order id"],
                ["has a dot", "order.id"],
            ] as const) {
                describe(`when it ${why}`, () => {
                    let el: UaiExtractionFieldConfigModalElement;
                    let submit: ReturnType<typeof vi.fn>;

                    beforeEach(async () => {
                        ({ el, submit } = await openModal({ kind: "text" }));
                        await fillAndSave(el, { alias });
                    });

                    it.skip("stays open", () => {
                        expect(submit).not.toHaveBeenCalled();
                    });

                    it.skip("says it must start with a letter and use only letters, numbers and _", () => {
                        expect(el.shadowRoot!.querySelector(".alias-error")?.textContent).toContain(
                            "start with a letter",
                        );
                    });
                });
            }
        });

        describe("Scenario: the alias is already used by another field", () => {
            let el: UaiExtractionFieldConfigModalElement;
            let submit: ReturnType<typeof vi.fn>;

            beforeEach(async () => {
                ({ el, submit } = await openModal({ kind: "text", otherAliases: ["orderId"] }));
                await fillAndSave(el, { alias: "orderId" });
            });

            it.skip("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it.skip("names the alias in the message", () => {
                expect(el.shadowRoot!.querySelector(".alias-error")?.textContent?.trim()).toBe(
                    'Another field already uses the alias "orderId".',
                );
            });
        });

        describe("Scenario: decimal places out of range", () => {
            let submit: ReturnType<typeof vi.fn>;

            beforeEach(async () => {
                let el: UaiExtractionFieldConfigModalElement;
                ({ el, submit } = await openModal({ kind: "number" }));
                setInput(el, "decimals", "7");
                await fillAndSave(el, { alias: "amount", instructions: "Amount paid" });
            });

            it.skip("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });
        });

        describe("Scenario: an edit is cancelled", () => {
            let reject: ReturnType<typeof vi.fn>;
            let setValue: ReturnType<typeof vi.fn>;

            beforeEach(async () => {
                let el: UaiExtractionFieldConfigModalElement;
                ({ el, reject, setValue } = await openModal({
                    kind: "text",
                    existingField: { kind: "text", alias: "orderId", instructions: "The order number" },
                }));
                setInput(el, "instructions", "Changed my mind");
                await el.updateComplete;
                await clickButton(el, "btn-cancel");
            });

            it.skip("closes without a value", () => {
                expect(reject).toHaveBeenCalledOnce();
            });

            it.skip("never hands back the changed field", () => {
                expect(setValue).not.toHaveBeenCalled();
            });
        });
    });
});
