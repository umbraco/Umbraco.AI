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
            return { setValidity: vi.fn(), setFormValue: vi.fn(), form: null } as unknown as ElementInternals;
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

    describe("Scenario: the alias field explains how to reference the value", () => {
        it("shows the literal binding example in the alias description", async () => {
            const { el } = await openModal({ kind: "text" });
            const layout = el.shadowRoot!.querySelector("umb-property-layout")!;
            const example = layout.querySelector('[slot="description"] code');
            expect(example?.textContent).toBe("${ steps.<stepAlias>.<alias>.value }");
        });
    });

    describe("Scenario: a Number field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "number" }));
        });

        it('shows the headline "Add field: Number"', () => {
            expect(headline(el)).toBe("Add field: Number");
        });

        it("shows Alias, Instructions, Decimal places, Whole number and Unit", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Decimal places", "Whole number", "Unit"]);
        });
    });

    describe("Scenario: a Duration field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "duration" }));
        });

        it("shows Alias, Instructions, Unit, Decimal places and Whole number", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Unit", "Decimal places", "Whole number"]);
        });

        it("defaults the unit to seconds", () => {
            expect(input(el, "unit")?.value).toBe("seconds");
        });
    });

    describe("Scenario: a List field is added", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({ kind: "list" }));
        });

        it("shows Alias, Instructions and Item type", () => {
            expect(fieldLabels(el)).toEqual(["Alias", "Instructions", "Item type"]);
        });

        it("defaults the item type to Text", () => {
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

                it("shows only Alias and Instructions", () => {
                    expect(fieldLabels(el)).toEqual(["Alias", "Instructions"]);
                });

                it(`shows the headline "Add field: ${title}"`, () => {
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

        it("submits", () => {
            expect(submit).toHaveBeenCalledOnce();
        });

        it("submits the exact flat field", () => {
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

        it("submits only kind, alias and instructions", () => {
            expect(submittedField(setValue)).toEqual({
                kind: "text",
                alias: "orderId",
                instructions: "The order number",
            });
        });
    });

    describe("Scenario: a Duration field is saved", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, setValue } = await openModal({ kind: "duration" }));
            await fillAndSave(el, { alias: "waited", instructions: "How long the customer waited" });
        });

        it("submits the exact flat field, with the default unit and integer", () => {
            expect(submittedField(setValue)).toEqual({
                kind: "duration",
                alias: "waited",
                instructions: "How long the customer waited",
                integer: false,
                unit: "seconds",
            });
        });
    });

    describe("Scenario: a List field is saved", () => {
        let setValue: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, setValue } = await openModal({ kind: "list" }));
            await fillAndSave(el, { alias: "damaged", instructions: "Damaged products" });
        });

        it("submits the exact flat field, with the default item type", () => {
            expect(submittedField(setValue)).toEqual({
                kind: "list",
                alias: "damaged",
                instructions: "Damaged products",
                itemType: "text",
            });
        });
    });

    describe("Scenario: the alias starts with an underscore", () => {
        let submit: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiExtractionFieldConfigModalElement;
            ({ el, submit } = await openModal({ kind: "text" }));
            await fillAndSave(el, { alias: "_orderId" });
        });

        it("submits", () => {
            expect(submit).toHaveBeenCalledOnce();
        });
    });

    describe("Scenario: a saved Number field is edited", () => {
        let el: UaiExtractionFieldConfigModalElement;

        beforeEach(async () => {
            ({ el } = await openModal({
                kind: "number",
                existingField: {
                    kind: "number",
                    alias: "amount",
                    instructions: "Amount paid",
                    decimals: 2,
                    unit: "DKK",
                },
                otherAliases: ["orderId"],
            }));
        });

        it('shows the headline "Edit field: Number"', () => {
            expect(headline(el)).toBe("Edit field: Number");
        });

        it("fills in the saved alias", () => {
            expect(input(el, "alias")?.value).toBe("amount");
        });

        it("fills in the saved instructions", () => {
            expect(input(el, "instructions")?.value).toBe("Amount paid");
        });

        it("fills in the saved decimal places", () => {
            expect(String(input(el, "decimals")?.value)).toBe("2");
        });

        it("fills in the saved unit", () => {
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

        it("submits", () => {
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

            it("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it("shows a message under Alias", () => {
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

            it("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it("shows a message under Instructions", () => {
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

                    it("stays open", () => {
                        expect(submit).not.toHaveBeenCalled();
                    });

                    it("says it must start with a letter or _ and use only letters, numbers and _", () => {
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

            it("stays open", () => {
                expect(submit).not.toHaveBeenCalled();
            });

            it("names the alias in the message", () => {
                expect(el.shadowRoot!.querySelector(".alias-error")?.textContent?.trim()).toBe(
                    'Another field already uses the alias "orderId".',
                );
            });
        });

        describe("Scenario: decimal places out of range", () => {
            for (const decimals of ["7", "1.5"]) {
                describe(`when it is ${decimals}`, () => {
                    let el: UaiExtractionFieldConfigModalElement;
                    let submit: ReturnType<typeof vi.fn>;

                    beforeEach(async () => {
                        ({ el, submit } = await openModal({ kind: "number" }));
                        setInput(el, "decimals", decimals);
                        await fillAndSave(el, { alias: "amount", instructions: "Amount paid" });
                    });

                    it("stays open", () => {
                        expect(submit).not.toHaveBeenCalled();
                    });

                    it("shows a message under Decimal places", () => {
                        expect(el.shadowRoot!.querySelector(".decimals-error")).not.toBeNull();
                    });
                });
            }
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

            it("closes without a value", () => {
                expect(reject).toHaveBeenCalledOnce();
            });

            it("never hands back the changed field", () => {
                expect(setValue).not.toHaveBeenCalled();
            });
        });
    });
});
