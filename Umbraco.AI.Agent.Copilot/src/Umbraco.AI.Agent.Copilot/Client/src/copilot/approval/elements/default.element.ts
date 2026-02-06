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
export class UaiAgentApprovalDefaultElement extends UmbLitElement implements UaiAgentApprovalElement {
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
        // const title = this.#localize.string(
        //   (this.config.title as string) ??
        //     (this.args.title as string) ??
        //     "#uaiAgentCopilot_approvalDefaultTitle"
        // );
        // const message = this.#localize.string(
        //   (this.config.message as string) ??
        //     (this.args.message as string) ??
        //     "#uaiAgentCopilot_approvalDefaultMessage"
        // );
        const approveLabel = this.#localize.string(
            (this.config.approveLabel as string) ??
                (this.args.approveLabel as string) ??
                "#uaiAgentCopilot_approvalApprove",
        );
        const denyLabel = this.#localize.string(
            (this.config.denyLabel as string) ?? (this.args.denyLabel as string) ?? "#uaiAgentCopilot_approvalDeny",
        );

        return html`
            <div class="actions">
                <uui-button look="primary" color="positive" @click=${this.#handleApprove}> ${approveLabel} </uui-button>
                <uui-button look="primary" @click=${this.#handleDeny}> ${denyLabel} </uui-button>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
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
