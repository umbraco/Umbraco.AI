import { customElement, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Regenerate button component for chat messages.
 *
 * @fires regenerate - Dispatched when the user clicks to regenerate the response
 */
@customElement("uai-message-regenerate-button")
export class UaiMessageRegenerateButtonElement extends UmbLitElement {
    #handleRegenerate() {
        this.dispatchEvent(new CustomEvent("regenerate", { bubbles: true, composed: true }));
    }

    override render() {
        return html`
            <uui-button compact look="secondary" @click=${this.#handleRegenerate} title="Regenerate">
                <uui-icon name="icon-sync"></uui-icon>
            </uui-button>
        `;
    }

    static override styles = css`
        :host {
            display: inline-block;
        }

        uui-button {
            --uui-button-height: 24px;
            --uui-button-font-size: 12px;
        }
    `;
}

export default UaiMessageRegenerateButtonElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-message-regenerate-button": UaiMessageRegenerateButtonElement;
    }
}
