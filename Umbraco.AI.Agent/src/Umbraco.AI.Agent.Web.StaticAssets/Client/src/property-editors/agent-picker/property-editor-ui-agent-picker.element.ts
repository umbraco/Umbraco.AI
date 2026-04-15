import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";

import type { UaiAgentPickerElement } from "../../agent/components/agent-picker/agent-picker.element.js";

const elementName = "uai-property-editor-ui-agent-picker";

@customElement(elementName)
export class UaiPropertyEditorUIAgentPickerElement
    extends UmbFormControlMixin<string | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    @state()
    private _surfaceId?: string;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;

        this._surfaceId = config.getValueByAlias<string>("surfaceId");
    }

    #onChange(e: UmbChangeEvent) {
        const target = e.target as UaiAgentPickerElement;
        this.value = target.value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <uai-agent-picker
                .value=${this.value}
                .surfaceId=${this._surfaceId}
                ?readonly=${this.readonly}
                @change=${this.#onChange}
            >
            </uai-agent-picker>
        `;
    }
}

export { UaiPropertyEditorUIAgentPickerElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIAgentPickerElement;
    }
}
