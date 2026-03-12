import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement } from "@umbraco-cms/backoffice/external/lit";

/**
 * A responsive grid layout wrapper for info cards.
 */
@customElement("uai-info-grid")
export class UaiInfoGridElement extends LitElement {
    render() {
        return html`<slot></slot>`;
    }

    static styles = css`
        :host {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 15px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-info-grid": UaiInfoGridElement;
    }
}
