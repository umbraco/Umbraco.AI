import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UaiAgentApprovalElement } from "../uai-agent-approval-element.extension.js";

/**
 * Default approval element with Approve/Deny buttons.
 *
 * Displays a confirmation dialog with customizable title, message, and buttons.
 * Priority order for display values: config → args → localized defaults
 *
 * @element uai-agent-approval-default
 */
@customElement("uai-agent-approval-default")
export class UaiAgentApprovalDefaultElement
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

  #handleApprove() {
    this.respond({ approved: true });
  }

  #handleDeny() {
    this.respond({ approved: false });
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
        "#uAiAgent_approval_defaultMessage"
    );
    const approveLabel = this.#localize.string(
      (this.config.approveLabel as string) ??
        (this.args.approveLabel as string) ??
        "#uAiAgent_approval_approve"
    );
    const denyLabel = this.#localize.string(
      (this.config.denyLabel as string) ??
        (this.args.denyLabel as string) ??
        "#uAiAgent_approval_deny"
    );

    return html`
      <uui-box .headline=${title}>
        <p class="message">${message}</p>
        <div class="actions">
          <uui-button
            look="primary"
            color="positive"
            @click=${this.#handleApprove}
          >
            ${approveLabel}
          </uui-button>
          <uui-button look="secondary" @click=${this.#handleDeny}>
            ${denyLabel}
          </uui-button>
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

    .actions {
      display: flex;
      gap: var(--uui-size-space-2);
    }
  `;
}

export default UaiAgentApprovalDefaultElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-agent-approval-default": UaiAgentApprovalDefaultElement;
  }
}
