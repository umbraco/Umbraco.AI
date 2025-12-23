import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UaiAgentApprovalElement } from "../uai-agent-approval-element.extension.js";

/**
 * Input approval element with a text field.
 *
 * Displays a prompt with a text input for user response.
 * Supports both single-line input and multiline textarea.
 * Priority order for display values: config → args → localized defaults
 *
 * @element uai-agent-approval-input
 */
@customElement("uai-agent-approval-input")
export class UaiAgentApprovalInputElement
  extends UmbLitElement
  implements UaiAgentApprovalElement
{
  readonly #localize = new UmbLocalizationController(this);

  @property({ type: Object })
  args: Record<string, unknown> = {};

  @property({ type: Object })
  config: Record<string, unknown> = {};

  @property({ attribute: false })
  respond!: (result: unknown) => void;

  @state()
  private _inputValue = "";

  #handleSubmit() {
    if (!this._inputValue.trim()) return;
    this.respond({ input: this._inputValue });
  }

  #handleCancel() {
    this.respond({ cancelled: true });
  }

  #handleKeydown(e: KeyboardEvent) {
    const multiline = (this.config.multiline ?? this.args.multiline) === true;
    if (e.key === "Enter" && !e.shiftKey && !multiline) {
      e.preventDefault();
      this.#handleSubmit();
    }
  }

  #handleInput(e: Event) {
    const target = e.target as HTMLInputElement | HTMLTextAreaElement;
    this._inputValue = target.value;
  }

  override render() {
    // Priority: config (manifest) → args (LLM) → localized default
    const prompt = this.#localize.string(
      (this.config.prompt as string) ??
        (this.args.prompt as string) ??
        "#uAiAgent_approval_defaultTitle"
    );
    const placeholder = this.#localize.string(
      (this.config.placeholder as string) ??
        (this.args.placeholder as string) ??
        "#uAiAgent_approval_inputPlaceholder"
    );
    const submitLabel = this.#localize.string(
      (this.config.submitLabel as string) ??
        (this.args.submitLabel as string) ??
        "#uAiAgent_approval_submit"
    );
    const cancelLabel = this.#localize.string(
      (this.config.cancelLabel as string) ??
        (this.args.cancelLabel as string) ??
        "#uAiAgent_approval_cancel"
    );
    const multiline = (this.config.multiline ?? this.args.multiline) === true;

    return html`
      <uui-box .headline=${prompt}>
        ${multiline
          ? html`
              <uui-textarea
                .value=${this._inputValue}
                .placeholder=${placeholder}
                @input=${this.#handleInput}
              ></uui-textarea>
            `
          : html`
              <uui-input
                .value=${this._inputValue}
                .placeholder=${placeholder}
                @input=${this.#handleInput}
                @keydown=${this.#handleKeydown}
              ></uui-input>
            `}
        <div class="actions">
          <uui-button look="primary" @click=${this.#handleSubmit}>
            ${submitLabel}
          </uui-button>
          <uui-button look="secondary" @click=${this.#handleCancel}>
            ${cancelLabel}
          </uui-button>
        </div>
      </uui-box>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    uui-input,
    uui-textarea {
      width: 100%;
      margin-bottom: var(--uui-size-space-4);
    }

    uui-textarea {
      min-height: 100px;
    }

    .actions {
      display: flex;
      gap: var(--uui-size-space-2);
    }
  `;
}

export default UaiAgentApprovalInputElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-agent-approval-input": UaiAgentApprovalInputElement;
  }
}
