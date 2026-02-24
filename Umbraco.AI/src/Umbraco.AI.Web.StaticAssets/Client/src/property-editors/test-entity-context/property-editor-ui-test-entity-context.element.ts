import { customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";

import type { UaiTestEntityContextElement } from "../../test/components/test-entity-context/test-entity-context.element.js";

const elementName = "uai-property-editor-ui-test-entity-context";

/**
 * Property editor wrapper for the test entity context component.
 * Delegates all logic to `<uai-test-entity-context>`.
 */
@customElement(elementName)
export class UaiPropertyEditorUITestEntityContextElement
    extends UmbFormControlMixin<string | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    #onChange(e: UmbChangeEvent) {
        const target = e.target as UaiTestEntityContextElement;
        this.value = target.value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <uai-test-entity-context
                .value=${this.value}
                ?readonly=${this.readonly}
                @change=${this.#onChange}
            >
            </uai-test-entity-context>
        `;
    }
}

export { UaiPropertyEditorUITestEntityContextElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUITestEntityContextElement;
    }
}
