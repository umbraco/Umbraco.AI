import { customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";

import type { UaiMockEntityElement, EntityContextValue } from "../../test/components/mock-entity/mock-entity.element.js";

const elementName = "uai-property-editor-ui-test-entity-context";

/**
 * Property editor wrapper for the mock entity component.
 * Delegates all logic to `<uai-mock-entity>`.
 */
@customElement(elementName)
export class UaiPropertyEditorUITestEntityContextElement
    extends UmbFormControlMixin<EntityContextValue | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    #onChange(e: UmbChangeEvent) {
        const target = e.target as UaiMockEntityElement;
        this.value = target.value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <uai-mock-entity
                .value=${this.value}
                ?readonly=${this.readonly}
                @change=${this.#onChange}
            >
            </uai-mock-entity>
        `;
    }
}

export { UaiPropertyEditorUITestEntityContextElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUITestEntityContextElement;
    }
}
