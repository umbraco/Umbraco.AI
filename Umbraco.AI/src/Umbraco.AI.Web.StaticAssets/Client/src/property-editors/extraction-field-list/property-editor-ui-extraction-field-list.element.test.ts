// EX-9 — Set up fields on an Extract Values step (AC1-AC3, AC8-AC11, AC16-AC18): the field list editor.
//
// Copies property-editor-ui-decision-question-list.element.test.ts. Assumed names (rename here if
// the build differs):
// - element `uai-property-editor-ui-extraction-field-list` (UaiPropertyEditorUIExtractionFieldListElement)
// - modal token UAI_EXTRACTION_FIELD_CONFIG_MODAL, data { kind, existingField?, otherAliases },
//   value { field }
// - row value UaiExtractionFieldListItem { kind, alias, instructions, decimals?, integer?, unit?, itemType? }
// - add button id `btn-add`; rows are `uui-ref-node`, like the Decision list.
// Strings use termOrDefault, so happy-dom renders the English defaults asserted below.
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_ITEM_PICKER_MODAL } from "../../core/modals/item-picker/item-picker-modal.token.js";
import { UaiSelectedEvent } from "../../core/events/selected.event.js";
import { UAI_EXTRACTION_FIELD_CONFIG_MODAL } from "./extraction-field-config-modal.token.js";
import "./property-editor-ui-extraction-field-list.element.js";
import type { UaiPropertyEditorUIExtractionFieldListElement } from "./property-editor-ui-extraction-field-list.element.js";
import type { UaiExtractionFieldListItem } from "./types.js";

// happy-dom doesn't implement ElementInternals, which UmbFormControlMixin needs. checkValidity is
// added (unlike the Decision list's stub) so the max-fields validator can be run.
beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), checkValidity: () => true, form: null } as unknown as ElementInternals;
        };
    }
});

type Field = UaiExtractionFieldListItem;

/** A scripted modal manager: records each open, and lets a test drive the picker and config modal. */
function provideModalManager(host: UmbControllerHost) {
    const opened: string[] = [];
    let pickerListener: ((e: Event) => void) | undefined;
    const picker = {
        addEventListener: (_type: string, cb: (e: Event) => void) => (pickerListener = cb),
        reject: vi.fn(),
        onSubmit: () => new Promise(() => {}),
    };
    let configOutcome: { field: Field } | "cancelled" | undefined;
    const open = vi.fn((_host: unknown, token: unknown) => {
        if (token === UAI_ITEM_PICKER_MODAL) {
            opened.push("picker");
            return picker;
        }
        opened.push("config");
        return {
            onSubmit: () =>
                configOutcome === "cancelled"
                    ? Promise.reject(new Error("cancelled"))
                    : Promise.resolve(configOutcome as { field: Field }),
        };
    });

    class FakeModalManagerContext extends UmbControllerBase {
        open = open;
        constructor(providerHost: UmbControllerHost) {
            super(providerHost);
            this.provideContext(UMB_MODAL_MANAGER_CONTEXT, this as never);
        }
    }
    new FakeModalManagerContext(host);

    return {
        opened,
        picker,
        open,
        submitConfig: (field: Field) => (configOutcome = { field }),
        cancelConfig: () => (configOutcome = "cancelled"),
        select: (value: string, label: string) => pickerListener?.(new UaiSelectedEvent(value, { value, label })),
    };
}

async function render(value?: Field[]) {
    const wrapper = document.createElement("div");
    document.body.appendChild(wrapper);
    const host = new UmbElementControllerHost(wrapper);
    host.hostConnected();
    const modals = provideModalManager(host);
    const el = document.createElement(
        "uai-property-editor-ui-extraction-field-list",
    ) as UaiPropertyEditorUIExtractionFieldListElement;
    if (value) el.value = value;
    wrapper.appendChild(el);
    await el.updateComplete;
    return { el, modals };
}

const settle = () => new Promise((r) => setTimeout(r));
const rows = (el: HTMLElement) => el.shadowRoot!.querySelectorAll("uui-ref-node");
const click = (target: Element | null | undefined) => target!.dispatchEvent(new MouseEvent("click"));
const pickerItems = (open: ReturnType<typeof vi.fn>) =>
    (open.mock.calls[0][2] as { data: { items: Array<{ label: string }> } }).data.items.map((i) => i.label);

const amount: Field = { kind: "number", alias: "amount", instructions: "Amount paid", decimals: 2, unit: "DKK" };
const orderId: Field = { kind: "text", alias: "orderId", instructions: "The order number" };
const damaged: Field = { kind: "list", alias: "damaged", instructions: "Damaged products", itemType: "text" };

describe("Feature: extraction field list editor", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: a new step has no fields", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;

        beforeEach(async () => {
            ({ el } = await render());
        });

        it('shows the "No fields yet" message', () => {
            expect(el.shadowRoot!.textContent).toContain("No fields yet");
        });

        it("shows an Add field button", () => {
            expect(el.shadowRoot!.getElementById("btn-add")?.getAttribute("label")).toBe("Add field");
        });

        it("shows no rows", () => {
            expect(rows(el).length).toBe(0);
        });
    });

    describe("Scenario: Add field is clicked", () => {
        let modals: ReturnType<typeof provideModalManager>;

        beforeEach(async () => {
            let el: UaiPropertyEditorUIExtractionFieldListElement;
            ({ el, modals } = await render());
            click(el.shadowRoot!.getElementById("btn-add"));
            await settle();
        });

        it("opens the type picker", () => {
            expect(modals.opened).toEqual(["picker"]);
        });

        it("offers the seven field types in order", () => {
            expect(pickerItems(modals.open)).toEqual([
                "Text",
                "Number",
                "Date",
                "Time",
                "Date and time",
                "Duration",
                "List",
            ]);
        });
    });

    describe("Scenario: Number is picked", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;
        let modals: ReturnType<typeof provideModalManager>;

        beforeEach(async () => {
            ({ el, modals } = await render([orderId]));
            modals.submitConfig(amount);
            click(el.shadowRoot!.getElementById("btn-add"));
            await settle();
            modals.select("number", "Number");
            await settle();
            await el.updateComplete;
        });

        it("opens the field modal for a Number field, passing the other aliases", () => {
            expect(modals.open).toHaveBeenLastCalledWith(el, UAI_EXTRACTION_FIELD_CONFIG_MODAL, {
                data: { kind: "number", otherAliases: ["orderId"] },
            });
        });

        it("closes the picker once the field is saved", () => {
            expect(modals.picker.reject).toHaveBeenCalledOnce();
        });

        it("adds a row", () => {
            expect(rows(el).length).toBe(2);
        });

        it("names the row after the instructions", () => {
            expect(rows(el)[1]!.getAttribute("name")).toBe("Amount paid");
        });

        it('shows "Type · alias" under the name', () => {
            expect(rows(el)[1]!.getAttribute("detail")).toBe("Number · amount");
        });

        it("adds the field to the end of the value", () => {
            expect(el.value).toEqual([orderId, amount]);
        });

        it("hides the empty message", () => {
            expect(el.shadowRoot!.textContent).not.toContain("No fields yet");
        });
    });

    describe("Scenario: a row is clicked", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;
        let modals: ReturnType<typeof provideModalManager>;

        beforeEach(async () => {
            ({ el, modals } = await render([orderId, amount]));
            modals.submitConfig(amount);
            rows(el)[1]!.dispatchEvent(new Event("open"));
            await settle();
            await el.updateComplete;
        });

        it("opens the field modal directly, without the picker", () => {
            expect(modals.opened).toEqual(["config"]);
        });

        it("passes the saved field and the other aliases to the modal", () => {
            expect(modals.open).toHaveBeenCalledWith(el, UAI_EXTRACTION_FIELD_CONFIG_MODAL, {
                data: { kind: "number", existingField: amount, otherAliases: ["orderId"] },
            });
        });
    });

    describe("Scenario: a row is edited and saved", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;
        let changeCount: number;

        beforeEach(async () => {
            let modals: ReturnType<typeof provideModalManager>;
            ({ el, modals } = await render([orderId, amount]));
            changeCount = 0;
            el.addEventListener(UmbChangeEvent.TYPE, () => changeCount++);
            modals.submitConfig({ ...amount, instructions: "Total paid" });
            click(rows(el)[1]!.querySelector('uui-button[label="Edit"]'));
            await settle();
            await el.updateComplete;
        });

        it("updates the row", () => {
            expect(rows(el)[1]!.getAttribute("name")).toBe("Total paid");
        });

        it("keeps the row in place", () => {
            expect(el.value?.map((f) => f.alias)).toEqual(["orderId", "amount"]);
        });

        it("fires a change event", () => {
            expect(changeCount).toBe(1);
        });
    });

    describe("Scenario: a row is removed", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;

        beforeEach(async () => {
            ({ el } = await render([orderId, amount]));
            click(rows(el)[0]!.querySelector('uui-button[label="Remove"]'));
            await el.updateComplete;
        });

        it("removes it from the list", () => {
            expect(el.value?.map((f) => f.alias)).toEqual(["amount"]);
        });
    });

    describe("Scenario: a step with three saved fields is reopened", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;

        beforeEach(async () => {
            ({ el } = await render([amount, orderId, damaged]));
        });

        it("shows the rows in the saved order", () => {
            expect(Array.from(rows(el)).map((r) => r.getAttribute("name"))).toEqual([
                "Amount paid",
                "The order number",
                "Damaged products",
            ]);
        });
    });

    describe("Scenario: 20 fields", () => {
        let el: UaiPropertyEditorUIExtractionFieldListElement;

        beforeEach(async () => {
            ({ el } = await render(
                Array.from({ length: 20 }, (_, i) => ({ kind: "text", alias: `f${i}`, instructions: "?" }) as Field),
            ));
            el.checkValidity();
        });

        it("is valid", () => {
            expect(el.validity.valid).toBe(true);
        });
    });

    describe("Sad path", () => {
        describe("Scenario: editing a field is cancelled", () => {
            let el: UaiPropertyEditorUIExtractionFieldListElement;

            beforeEach(async () => {
                let modals: ReturnType<typeof provideModalManager>;
                ({ el, modals } = await render([orderId, amount]));
                modals.cancelConfig();
                click(rows(el)[1]!.querySelector('uui-button[label="Edit"]'));
                await settle();
                await el.updateComplete;
            });

            it("leaves the saved field unchanged", () => {
                expect(el.value).toEqual([orderId, amount]);
            });
        });

        describe("Scenario: the Add field modal is cancelled", () => {
            let el: UaiPropertyEditorUIExtractionFieldListElement;
            let modals: ReturnType<typeof provideModalManager>;

            beforeEach(async () => {
                ({ el, modals } = await render());
                modals.cancelConfig();
                click(el.shadowRoot!.getElementById("btn-add"));
                await settle();
                modals.select("number", "Number");
                await settle();
                await el.updateComplete;
            });

            it("leaves the type picker open", () => {
                expect(modals.picker.reject).not.toHaveBeenCalled();
            });

            it("adds nothing", () => {
                expect(rows(el).length).toBe(0);
            });
        });

        describe("Scenario: more than 20 fields", () => {
            let el: UaiPropertyEditorUIExtractionFieldListElement;

            beforeEach(async () => {
                ({ el } = await render(
                    Array.from(
                        { length: 21 },
                        (_, i) => ({ kind: "text", alias: `f${i}`, instructions: "?" }) as Field,
                    ),
                ));
                el.checkValidity();
            });

            it("is invalid, which blocks saving the step", () => {
                expect(el.validity.valid).toBe(false);
            });
        });
    });
});
