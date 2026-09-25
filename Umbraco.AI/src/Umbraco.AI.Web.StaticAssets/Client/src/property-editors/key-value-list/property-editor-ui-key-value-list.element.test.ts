// PLAN T27 — reusable key/value list property editor UI
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbPropertyEditorConfigCollection } from "@umbraco-cms/backoffice/property-editor";
import { UmbSorterController } from "@umbraco-cms/backoffice/sorter";
import "./property-editor-ui-key-value-list.element.js";
import type { UaiPropertyEditorUIKeyValueListElement } from "./property-editor-ui-key-value-list.element.js";

// happy-dom (as pinned in this workspace) doesn't implement the Element Internals API that
// UmbFormControlMixin's constructor requires (`this.attachInternals()`), so any form-control
// element fails to construct under vitest without this stub. Scoped to this file rather than a
// shared setup file, since no other property editor has direct-instantiation specs yet.
//
// `setValidity` is a spy (not a no-op) so the min/max validator specs below can assert what the
// form control actually reported, rather than just that construction didn't throw. Each element
// gets its own internals object, tracked by instance so a test can look up the right one.
const internalsByElement = new WeakMap<HTMLElement, { setValidity: ReturnType<typeof vi.fn> }>();

beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function (this: HTMLElement) {
            const internals = { setValidity: vi.fn(), form: null };
            internalsByElement.set(this, internals);
            return internals as unknown as ElementInternals;
        };
    }
});

async function renderElement(value?: Array<{ key: string; value: string }>) {
    const el = document.createElement(
        "uai-property-editor-ui-key-value-list",
    ) as UaiPropertyEditorUIKeyValueListElement;
    if (value) el.value = value;
    document.body.appendChild(el);
    await el.updateComplete;
    return el;
}

const rows = (el: UaiPropertyEditorUIKeyValueListElement) => el.shadowRoot!.querySelectorAll(".row");
const keyInput = (el: UaiPropertyEditorUIKeyValueListElement, index: number) =>
    rows(el)[index]?.querySelector<HTMLElement & { value?: string }>(".key-input");
const removeButton = (el: UaiPropertyEditorUIKeyValueListElement, index: number) =>
    rows(el)[index]?.querySelector<HTMLElement>("uui-button");

/** Last validity flags/message reported to the element's (stubbed) ElementInternals, if any. */
function lastValidityCall(el: UaiPropertyEditorUIKeyValueListElement) {
    const internals = internalsByElement.get(el);
    const calls = internals?.setValidity.mock.calls ?? [];
    return calls[calls.length - 1] as [Record<string, boolean>, string | undefined] | undefined;
}

describe("Feature: AI key/value list property editor", () => {
    afterEach(() => {
        document.body.innerHTML = "";
        // UmbSorterController.activeItem is a static, shared across every sorter instance in the
        // process — clear it so a reorder driven in one test can't leak into the next.
        UmbSorterController.activeItem = undefined;
    });

    describe("Scenario: an initial value is provided", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([{ key: "a", value: "Apple" }]);
        });

        it("renders one row per entry", () => {
            expect(rows(el).length).toBe(1);
        });

        it("renders the entry's key in the key input", () => {
            expect(keyInput(el, 0)?.value).toBe("a");
        });
    });

    describe("Scenario: a row is added", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([]);
            el.shadowRoot!.getElementById("btn-add")!.dispatchEvent(new MouseEvent("click"));
            await el.updateComplete;
        });

        it("renders a new blank row", () => {
            expect(rows(el).length).toBe(1);
        });
    });

    describe("Scenario: a row is removed", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([
                { key: "a", value: "Apple" },
                { key: "b", value: "Banana" },
            ]);
            rows(el)[0]!.querySelector("uui-button")!.dispatchEvent(new MouseEvent("click"));
            await el.updateComplete;
        });

        it("removes the row from the list", () => {
            expect(rows(el).length).toBe(1);
        });
    });

    describe("Scenario: all rows are removed", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([{ key: "a", value: "Apple" }]);
            removeButton(el, 0)!.dispatchEvent(new MouseEvent("click"));
            await el.updateComplete;
        });

        it("emits an undefined value rather than an empty array", () => {
            expect(el.value).toBeUndefined();
        });
    });

    describe("Scenario: a row's key is edited", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;
        let changeCount: number;

        beforeEach(async () => {
            el = await renderElement([{ key: "a", value: "Apple" }]);
            changeCount = 0;
            el.addEventListener(UmbChangeEvent.TYPE, () => {
                changeCount++;
            });
            const input = keyInput(el, 0)!;
            input.value = "b";
            input.dispatchEvent(new Event("input"));
        });

        it("fires a change event", () => {
            expect(changeCount).toBe(1);
        });

        it("updates the emitted value with the new key", () => {
            expect(el.value).toEqual([{ key: "b", value: "Apple" }]);
        });
    });

    describe("Scenario: a row is edited then the list is reordered", () => {
        // UmbSorterController only ever calls its `onChange` (which is what a real drag ends
        // with) from a native HTML5 drag-and-drop sequence, which needs a real `DataTransfer` and
        // real element layout geometry — neither of which happy-dom provides, so a real drag
        // can't be simulated here. `moveItemInModel` is the same controller method a real drag
        // ends by calling, so driving it directly still exercises the element's real `onChange`
        // path (`this._rows = model`) rather than asserting against the sorter's internals.
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([
                { key: "a", value: "Apple" },
                { key: "b", value: "Banana" },
            ]);
            const input = keyInput(el, 0)!;
            input.value = "aa";
            input.dispatchEvent(new Event("input"));
            await el.updateComplete;

            // Move the edited row (index 0) to after row 1 — `moveItemInModel` needs the
            // controller's static `activeItem` set first, mirroring what a real drag sets as
            // soon as it starts.
            UmbSorterController.activeItem = el._sorter.getModel()[0];
            await el._sorter.moveItemInModel(2, el._sorter);
        });

        it("emits the edited text in its new, reordered position", () => {
            expect(el.value).toEqual([
                { key: "b", value: "Banana" },
                { key: "aa", value: "Apple" },
            ]);
        });
    });

    describe("Scenario: the editor is readonly", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([{ key: "a", value: "Apple" }]);
            el.readonly = true;
            await el.updateComplete;
        });

        it("hides the remove control", () => {
            expect(removeButton(el, 0)).toBeNull();
        });

        it("hides the add button", () => {
            expect(el.shadowRoot!.getElementById("btn-add")).toBeNull();
        });
    });

    describe("Scenario: fewer rows than the configured minimum", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([{ key: "a", value: "Apple" }]);
            el.config = new UmbPropertyEditorConfigCollection([{ alias: "min", value: 2 }]);
            await el.updateComplete;
        });

        it("reports a rangeUnderflow validity error", () => {
            const [flags] = lastValidityCall(el) ?? [{} as Record<string, boolean>];
            expect(flags.rangeUnderflow).toBe(true);
        });
    });

    describe("Scenario: more rows than the configured maximum", () => {
        let el: UaiPropertyEditorUIKeyValueListElement;

        beforeEach(async () => {
            el = await renderElement([
                { key: "a", value: "Apple" },
                { key: "b", value: "Banana" },
            ]);
            el.config = new UmbPropertyEditorConfigCollection([{ alias: "max", value: 1 }]);
            await el.updateComplete;
        });

        it("reports a rangeOverflow validity error", () => {
            const [flags] = lastValidityCall(el) ?? [{} as Record<string, boolean>];
            expect(flags.rangeOverflow).toBe(true);
        });
    });
});
