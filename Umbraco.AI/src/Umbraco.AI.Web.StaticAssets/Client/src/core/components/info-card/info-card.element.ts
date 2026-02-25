import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property } from "@umbraco-cms/backoffice/external/lit";

/**
 * A card with background/border chrome wrapping a labeled field.
 */
@customElement("uai-info-card")
export class UaiInfoCardElement extends LitElement {
    @property({ type: String })
    label = "";

    render() {
        return html`
            <uai-labeled-field label=${this.label}>
                <slot></slot>
            </uai-labeled-field>
        `;
    }

    static styles = css`
        :host {
            display: block;
            background: var(--uui-color-surface);
            padding: 15px;
            border-radius: 6px;
            border: 1px solid var(--uui-color-border);
        }

        ::slotted(*) {
            word-break: break-all;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-info-card": UaiInfoCardElement;
    }
}
