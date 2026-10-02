// DR-16 — Ask several questions in one Automate step (AC1-AC4): the question-list editor.
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_ITEM_PICKER_MODAL } from "../../core/modals/item-picker/item-picker-modal.token.js";
import { UaiSelectedEvent } from "../../core/events/selected.event.js";
import "./property-editor-ui-decision-question-list.element.js";
import type { UaiPropertyEditorUIDecisionQuestionListElement } from "./property-editor-ui-decision-question-list.element.js";
import type { UaiDecisionQuestionListItem } from "./types.js";

// happy-dom doesn't implement ElementInternals, which UmbFormControlMixin needs at construction.
// Same stub as the key/value list editor's spec.
beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), form: null } as unknown as ElementInternals;
        };
    }
});

type Question = UaiDecisionQuestionListItem;

/** A scripted modal manager: records each open, and lets a test drive the picker and config modal. */
function provideModalManager(host: UmbControllerHost) {
    const opened: string[] = [];
    let pickerListener: ((e: Event) => void) | undefined;
    const picker = {
        addEventListener: (_type: string, cb: (e: Event) => void) => (pickerListener = cb),
        reject: vi.fn(),
        onSubmit: () => new Promise(() => {}),
    };
    // Resolved lazily inside onSubmit() (rather than pre-building a rejected Promise here) so a
    // cancelled config doesn't register as an unhandled rejection before anything awaits it.
    let configOutcome: { question: Question } | "cancelled" | undefined;
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
                    : Promise.resolve(configOutcome as { question: Question }),
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
        submitConfig: (question: Question) => (configOutcome = { question }),
        cancelConfig: () => (configOutcome = "cancelled"),
        // Matches the real picker's own dispatch: `new UaiSelectedEvent(item.value, item)`.
        select: (value: string, label: string) => pickerListener?.(new UaiSelectedEvent(value, { value, label })),
    };
}

async function render(value?: Question[]) {
    const wrapper = document.createElement("div");
    document.body.appendChild(wrapper);
    const host = new UmbElementControllerHost(wrapper);
    host.hostConnected();
    const modals = provideModalManager(host);
    const el = document.createElement(
        "uai-property-editor-ui-decision-question-list",
    ) as UaiPropertyEditorUIDecisionQuestionListElement;
    if (value) el.value = value;
    wrapper.appendChild(el);
    await el.updateComplete;
    return { el, modals };
}

const settle = () => new Promise((r) => setTimeout(r));
const rows = (el: HTMLElement) => el.shadowRoot!.querySelectorAll("uui-ref-node");
const click = (target: Element | null | undefined) => target!.dispatchEvent(new MouseEvent("click"));

const refund: Question = { kind: "binary", alias: "refund", instructions: "Does the customer want a refund?" };

describe("Feature: decision question list editor", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: a yes/no question is added through the picker and config modal", () => {
        let el: UaiPropertyEditorUIDecisionQuestionListElement;
        let modals: ReturnType<typeof provideModalManager>;

        beforeEach(async () => {
            ({ el, modals } = await render());
            modals.submitConfig(refund);
            click(el.shadowRoot!.getElementById("btn-add"));
            await settle();
            modals.select("binary", "Yes/no");
            await settle();
            await el.updateComplete;
        });

        it("opens the picker before the config modal", () => {
            expect(modals.opened).toEqual(["picker", "config"]);
        });

        it("adds a row", () => {
            expect(rows(el).length).toBe(1);
        });

        it("names the row after the instructions", () => {
            expect(rows(el)[0]!.getAttribute("name")).toBe("Does the customer want a refund?");
        });

        it("shows the kind and alias as the row detail", () => {
            expect(rows(el)[0]!.getAttribute("detail")).toBe("Yes/no · refund");
        });

        it("closes the picker", () => {
            expect(modals.picker.reject).toHaveBeenCalledOnce();
        });

        it("adds the question to the value", () => {
            expect(el.value).toEqual([refund]);
        });
    });

    describe("Scenario: the config modal is cancelled", () => {
        let el: UaiPropertyEditorUIDecisionQuestionListElement;
        let modals: ReturnType<typeof provideModalManager>;

        beforeEach(async () => {
            ({ el, modals } = await render());
            modals.cancelConfig();
            click(el.shadowRoot!.getElementById("btn-add"));
            await settle();
            modals.select("binary", "Yes/no");
            await settle();
            await el.updateComplete;
        });

        it("leaves the picker open", () => {
            expect(modals.picker.reject).not.toHaveBeenCalled();
        });

        it("adds nothing", () => {
            expect(rows(el).length).toBe(0);
        });
    });

    describe("Scenario: a row is edited", () => {
        let el: UaiPropertyEditorUIDecisionQuestionListElement;
        let modals: ReturnType<typeof provideModalManager>;
        let changeCount: number;

        beforeEach(async () => {
            ({ el, modals } = await render([refund]));
            changeCount = 0;
            el.addEventListener(UmbChangeEvent.TYPE, () => changeCount++);
            modals.submitConfig({ ...refund, instructions: "Is a refund requested?" });
            click(rows(el)[0]!.querySelector('uui-button[label="Edit"]'));
            await settle();
            await el.updateComplete;
        });

        it("opens the config modal directly, without the picker", () => {
            expect(modals.opened).toEqual(["config"]);
        });

        it("updates the row", () => {
            expect(rows(el)[0]!.getAttribute("name")).toBe("Is a refund requested?");
        });

        it("fires a change event", () => {
            expect(changeCount).toBe(1);
        });
    });

    describe("Scenario: a row is removed", () => {
        let el: UaiPropertyEditorUIDecisionQuestionListElement;

        beforeEach(async () => {
            ({ el } = await render([refund, { kind: "choice", alias: "category", instructions: "What about?" }]));
            click(rows(el)[0]!.querySelector('uui-button[label="Remove"]'));
            await el.updateComplete;
        });

        it("removes it from the value", () => {
            expect(el.value?.map((q) => q.alias)).toEqual(["category"]);
        });
    });
});
