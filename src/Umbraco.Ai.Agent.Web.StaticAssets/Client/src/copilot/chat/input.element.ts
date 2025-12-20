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
      <div class="input-wrapper">
        <div class="input-box">
          <uui-textarea
            .value=${this._value}
            placeholder=${this.placeholder}
            ?disabled=${this.disabled}
            auto-height
            @input=${this.#handleInput}
            @keydown=${this.#handleKeydown}
          ></uui-textarea>
          <hr class="divider" />
          <div class="actions-row">
            <div class="left-actions">
              <!-- Future: add attachment, voice, etc. -->
            </div>
            <uui-button
              look="primary"
              compact
              ?disabled=${this.disabled || !this._value.trim()}
              @click=${this.#send}
            >
              <uui-icon name="icon-navigation-right"></uui-icon>
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .input-wrapper {
      padding: var(--uui-size-space-3);
      border-top: 1px solid var(--uui-color-border);
      background: var(--uui-color-surface-alt);
    }

    .input-box {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-3);
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
    }

    uui-textarea {
      --uui-textarea-min-height: 24px;
      --uui-textarea-max-height: 200px;
      --uui-textarea-background-color: transparent;
      --uui-textarea-border-color: transparent;
    }

    uui-textarea:focus-within {
      --uui-textarea-border-color: transparent;
    }

    .divider {
      border: none;
      border-top: 1px solid var(--uui-color-border);
      margin: 0;
    }

    .actions-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .left-actions {
      display: flex;
      gap: var(--uui-size-space-2);
    }
  `;
}

export default UaiCopilotInputElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-input": UaiCopilotInputElement;
  }
}
