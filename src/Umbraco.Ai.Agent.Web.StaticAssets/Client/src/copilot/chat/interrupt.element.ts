import { customElement, property, state, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { InterruptInfo, InterruptOption } from "./types.js";

/**
 * Interrupt UI component.
 * Renders approval/input/choice UI for human-in-the-loop interactions.
 *
 * @fires respond - Dispatched when user responds to the interrupt
 */
@customElement("uai-copilot-interrupt")
export class UaiCopilotInterruptElement extends UmbLitElement {
  @property({ type: Object })
  interrupt?: InterruptInfo;

  @state()
  private _inputValue = "";

  #handleResponse(value: string) {
    this.dispatchEvent(
      new CustomEvent("respond", {
        detail: value,
        bubbles: true,
        composed: true,
      })
    );
  }

  #handleInputKeydown(e: KeyboardEvent) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      this.#submitInput();
    }
  }

  #submitInput() {
    if (!this._inputValue.trim()) return;
    this.#handleResponse(this._inputValue);
    this._inputValue = "";
  }

  #renderApproval() {
    const options = this.interrupt?.options ?? [
      { value: "yes", label: "Yes", variant: "positive" as const },
      { value: "no", label: "No", variant: "default" as const },
    ];

    return html`
      <div class="interrupt-actions">
        ${options.map((opt) => this.#renderButton(opt))}
      </div>
    `;
  }

  #renderButton(opt: InterruptOption) {
    const look = opt.variant === "danger" ? "primary" : "secondary";
    const color = opt.variant === "danger" ? "danger" : opt.variant === "positive" ? "positive" : "default";

    return html`
      <uui-button
        look=${look}
        color=${color}
        @click=${() => this.#handleResponse(opt.value)}
      >
        ${opt.label}
      </uui-button>
    `;
  }

  #renderInput() {
    const config = this.interrupt?.inputConfig;

    if (config?.multiline) {
      return html`
        <div class="interrupt-input">
          <uui-textarea
            .value=${this._inputValue}
            placeholder=${config.placeholder ?? "Enter your response..."}
            @input=${(e: Event) => (this._inputValue = (e.target as HTMLTextAreaElement).value)}
          ></uui-textarea>
          <uui-button look="primary" @click=${this.#submitInput}>
            Submit
          </uui-button>
        </div>
      `;
    }

    return html`
      <div class="interrupt-input">
        <uui-input
          .value=${this._inputValue}
          placeholder=${config?.placeholder ?? "Enter your response..."}
          @input=${(e: Event) => (this._inputValue = (e.target as HTMLInputElement).value)}
          @keydown=${this.#handleInputKeydown}
        ></uui-input>
        <uui-button look="primary" @click=${this.#submitInput}>
          Submit
        </uui-button>
      </div>
    `;
  }

  #renderChoice() {
    return html`
      <div class="interrupt-choices">
        ${this.interrupt?.options?.map(
          (opt) => html`
            <uui-button look="outline" @click=${() => this.#handleResponse(opt.value)}>
              ${opt.label}
            </uui-button>
          `
        )}
      </div>
    `;
  }

  #renderContent() {
    switch (this.interrupt?.type) {
      case "approval":
        return this.#renderApproval();
      case "input":
        return this.#renderInput();
      case "choice":
        return this.#renderChoice();
      case "custom":
      default:
        // For custom types, render options if available, otherwise input
        return this.interrupt?.options?.length
          ? this.#renderChoice()
          : this.#renderInput();
    }
  }

  override render() {
    if (!this.interrupt) {
      return nothing;
    }

    return html`
      <div class="interrupt-card">
        <div class="interrupt-header">
          <uui-icon name="icon-alert"></uui-icon>
          <span>${this.interrupt.title}</span>
        </div>
        <div class="interrupt-message">${this.interrupt.message}</div>
        ${this.#renderContent()}
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .interrupt-card {
      margin: var(--uui-size-space-3);
      padding: var(--uui-size-space-4);
      background: var(--uui-color-surface-alt);
      border: 1px solid var(--uui-color-warning);
      border-radius: var(--uui-border-radius);
    }

    .interrupt-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      font-weight: 600;
      color: var(--uui-color-warning-standalone);
      margin-bottom: var(--uui-size-space-3);
    }

    .interrupt-message {
      margin-bottom: var(--uui-size-space-4);
      line-height: 1.5;
    }

    .interrupt-actions {
      display: flex;
      gap: var(--uui-size-space-2);
      flex-wrap: wrap;
    }

    .interrupt-input {
      display: flex;
      gap: var(--uui-size-space-2);
    }

    .interrupt-input uui-input,
    .interrupt-input uui-textarea {
      flex: 1;
    }

    .interrupt-choices {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
    }

    .interrupt-choices uui-button {
      width: 100%;
    }
  `;
}

export default UaiCopilotInterruptElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-interrupt": UaiCopilotInterruptElement;
  }
}
