import { customElement, property, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UaiAgentApprovalElement } from "../uai-agent-approval-element.extension.js";

/**
 * Choice option for multiple choice approval.
 */
export interface ApprovalChoiceOption {
  /** Value to return when selected */
  value: string;
  /** Display label */
  label: string;
  /** Optional description */
  description?: string;
  /** Button variant: positive, danger, or default */
  variant?: "positive" | "danger" | "default";
}

/**
 * Choice approval element with multiple options.
 *
 * Displays a prompt with a list of buttons for user selection.
 * Options can have different variants (positive, danger, default) for styling.
 * Priority order for display values: config → args → localized defaults
 *
 * @element uai-agent-approval-choice
 */
@customElement("uai-agent-approval-choice")
export class UaiAgentApprovalChoiceElement
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

  #handleChoice(value: string) {
    this.respond({ choice: value });
  }

  #renderOption(option: ApprovalChoiceOption) {
    const look = option.variant === "danger" ? "primary" : "outline";
    const color = option.variant === "danger"
      ? "danger"
      : option.variant === "positive"
        ? "positive"
        : "default";

    return html`
      <uui-button
        class="choice-button"
        look=${look}
        color=${color}
        @click=${() => this.#handleChoice(option.value)}
      >
        <span class="choice-label">${option.label}</span>
        ${option.description
          ? html`<span class="choice-description">${option.description}</span>`
          : nothing}
      </uui-button>
    `;
  }

  override render() {
    // Priority: config (manifest) → args (LLM) → localized default
    const title = this.#localize.string(
      (this.config.title as string) ??
        (this.args.title as string) ??
        "#uAiAgent_approval_defaultTitle"
    );
    const message = this.#localize.string(
      (this.config.message as string) ??
        (this.args.message as string) ??
        ""
    );

    // Options from config or args
    const options = (this.config.options ?? this.args.options) as
      | ApprovalChoiceOption[]
      | undefined;

    if (!options?.length) {
      return html`
        <uui-box .headline=${title}>
          <p class="message">${message || "No options available."}</p>
        </uui-box>
      `;
    }

    return html`
      <uui-box .headline=${title}>
        ${message ? html`<p class="message">${message}</p>` : nothing}
        <div class="choices">
          ${options.map((opt) => this.#renderOption(opt))}
        </div>
      </uui-box>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .message {
      margin: 0 0 var(--uui-size-space-4) 0;
      line-height: 1.5;
    }

    .choices {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
    }

    .choice-button {
      width: 100%;
      text-align: left;
    }

    .choice-label {
      font-weight: 500;
    }

    .choice-description {
      display: block;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
      font-weight: normal;
    }
  `;
}

export default UaiAgentApprovalChoiceElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-agent-approval-choice": UaiAgentApprovalChoiceElement;
  }
}
