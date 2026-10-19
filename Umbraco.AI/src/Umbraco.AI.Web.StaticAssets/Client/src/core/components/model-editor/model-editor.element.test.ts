import { afterEach, beforeAll, describe, expect, it } from "vitest";
import "./model-editor.element.js";
import type { UaiModelEditorElement } from "./model-editor.element.js";
import type { UaiEditableModelSchemaModel } from "../../types.js";

const schema: UaiEditableModelSchemaModel = {
    fields: [
        { key: "alias", label: "Alias", sortOrder: 1, isRequired: true },
        { key: "flag", label: "Flag", sortOrder: 2, isRequired: false, defaultValue: true },
    ],
};

async function render(inheritMode: boolean) {
    const changes: Array<Record<string, unknown>> = [];
    const el = document.createElement("uai-model-editor") as UaiModelEditorElement;
    el.addEventListener("change", (e) => changes.push((e as CustomEvent).detail.model));
    el.inheritMode = inheritMode;
    el.schema = schema;
    el.model = {};
    document.body.appendChild(el);
    await el.updateComplete;
    await new Promise((r) => setTimeout(r));
    await el.updateComplete;
    return { el, changes };
}

const properties = (el: HTMLElement) =>
    Array.from(el.shadowRoot!.querySelectorAll("umb-property")) as Array<
        HTMLElement & { validation?: { mandatory: boolean } }
    >;

describe("Feature: model editor inherit mode", () => {
    beforeAll(() => {
        if (!HTMLElement.prototype.attachInternals) {
            HTMLElement.prototype.attachInternals = function () {
                return { setValidity: () => {}, form: null } as unknown as ElementInternals;
            };
        }
    });
    afterEach(() => {
        document.body.innerHTML = "";
    });

    it("marks required fields mandatory and applies defaults by default", async () => {
        const { el, changes } = await render(false);
        expect(properties(el)[0].validation?.mandatory).toBe(true);
        expect(changes[0]).toEqual({ alias: undefined, flag: true });
    });

    it("drops mandatory in inherit mode", async () => {
        const { el } = await render(true);
        expect(properties(el).every((p) => p.validation?.mandatory === false)).toBe(true);
    });

    it("does not populate defaults in inherit mode", async () => {
        const { changes } = await render(true);
        expect(changes).toHaveLength(0);
    });
});
