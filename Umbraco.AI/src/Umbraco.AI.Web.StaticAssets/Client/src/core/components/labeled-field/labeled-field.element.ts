import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property } from "@umbraco-cms/backoffice/external/lit";

/**
 * A label + slotted content pair. No visual chrome — just the label/content layout.
 */
@customElement("uai-labeled-field")
export class UaiLabeledFieldElement extends LitElement {
    @property({ type: String })
    label = "";

    render() {
        return html`
            <label>${this.label}</label>
            <div class="content"><slot></slot></div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        label {
            display: block;
            font-size: 12px;
            text-transform: uppercase;
            color: var(--uui-color-text-alt);
            opacity: 0.6;
            margin-bottom: 5px;
            font-weight: 700;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-labeled-field": UaiLabeledFieldElement;
    }
}
