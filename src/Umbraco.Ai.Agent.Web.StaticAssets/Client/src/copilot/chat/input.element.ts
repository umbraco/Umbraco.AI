import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Chat input component.
 * Provides a text input with send button and keyboard support.
 *
 * @fires send - Dispatched when user sends a message
 */
@customElement("uai-copilot-input")
export class UaiCopilotInputElement extends UmbLitElement {
  @property({ type: Boolean })
  disabled = false;

  @property({ type: String })
  placeholder = "Type a message...";

  @state()
  private _value = "";

  #handleKeydown(e: KeyboardEvent) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      this.#send();
    }
  }

  #handleInput(e: Event) {
    this._value = (e.target as HTMLTextAreaElement).value;
  }

  #send() {
    if (!this._value.trim() || this.disabled) return;

    this.dispatchEvent(
      new CustomEvent("send", {
        detail: this._value,
        bubbles: true,
        composed: true,
      })
    );

    this._value = "";
  }

  override render() {
    return html`
      <div class="input-container">
        <uui-textarea
          .value=${this._value}
          placeholder=${this.placeholder}
          ?disabled=${this.disabled}
          auto-height
          @input=${this.#handleInput}
          @keydown=${this.#handleKeydown}
        ></uui-textarea>
        <uui-button
          look="primary"
          compact
          ?disabled=${this.disabled || !this._value.trim()}
          @click=${this.#send}
        >
          <uui-icon name="icon-navigation-right"></uui-icon>
        </uui-button>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .input-container {
      display: flex;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-3);
      border-top: 1px solid var(--uui-color-border);
      background: var(--uui-color-surface);
    }

    uui-textarea {
      flex: 1;
      --uui-textarea-min-height: 40px;
      --uui-textarea-max-height: 200px;
    }

    uui-button {
      align-self: flex-end;
    }
  `;
}

export default UaiCopilotInputElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-input": UaiCopilotInputElement;
  }
}
