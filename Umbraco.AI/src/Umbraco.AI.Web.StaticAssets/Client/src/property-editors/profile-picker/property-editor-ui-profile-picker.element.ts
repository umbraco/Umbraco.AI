import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";

import type { UaiProfilePickerElement } from "../../profile/components/profile-picker/profile-picker.element.js";

const elementName = "uai-property-editor-ui-profile-picker";

@customElement(elementName)
export class UaiPropertyEditorUIProfilePickerElement
    extends UmbFormControlMixin<string | string[] | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    @state()
    private _capability?: string;

    @state()
    private _multiple = false;

    @state()
    private _min?: number;

    @state()
    private _max?: number;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;

        this._capability = config.getValueByAlias<string>("capability");
        this._multiple = config.getValueByAlias<boolean>("multiple") ?? false;
        this._min = config.getValueByAlias<number>("min");
        this._max = config.getValueByAlias<number>("max");
    }

    #onChange(e: UmbChangeEvent) {
        const target = e.target as UaiProfilePickerElement;
        this.value = target.value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <uai-profile-picker
                .value=${this.value}
                .capability=${this._capability}
                ?multiple=${this._multiple}
                ?readonly=${this.readonly}
                .min=${this._min}
                .max=${this._max}
                @change=${this.#onChange}
            >
            </uai-profile-picker>
        `;
    }
}

export { UaiPropertyEditorUIProfilePickerElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIProfilePickerElement;
    }
}
