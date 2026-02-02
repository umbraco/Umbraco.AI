import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Copy button component for chat messages.
 * Copies the provided content to clipboard and shows visual feedback.
 */
@customElement("uai-message-copy-button")
export class UaiMessageCopyButtonElement extends UmbLitElement {
  @property({ type: String })
  content = "";

  @state()
  private _copied = false;

  async #handleCopy() {
    if (!this.content) return;

    try {
      await navigator.clipboard.writeText(this.content);
      this._copied = true;
      setTimeout(() => (this._copied = false), 2000);
    } catch (err) {
      console.error("Failed to copy message:", err);
    }
  }

  override render() {
    return html`
      <uui-button compact look="secondary" @click=${this.#handleCopy} title="Copy">
        <uui-icon name="${this._copied ? "icon-check" : "icon-documents"}"></uui-icon>
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

export default UaiMessageCopyButtonElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-message-copy-button": UaiMessageCopyButtonElement;
  }
}
