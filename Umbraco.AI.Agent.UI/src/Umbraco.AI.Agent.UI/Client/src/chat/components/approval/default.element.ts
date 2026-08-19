import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UaiAgentApprovalElement } from "../../extensions/uai-agent-approval-element.extension.js";

/**
 * Default approval element with Approve/Deny buttons.
 *
 * Displays a confirmation dialog with customizable buttons.
 * Priority order for display values: config -> args -> localized defaults
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
        const rawTitle = (this.config.title as string) ?? (this.args.title as string);
        const rawMessage = (this.config.message as string) ?? (this.args.message as string);
        // Title/message may be a localization key (e.g. "#uaiChat_approvalDefaultTitle") set by a
        // frontend tool's approval config, or a literal string built server-side for a backend tool
        // (e.g. "Set 'title' to 'New Title'."). #localize.string() resolves the former and passes the
        // latter through unchanged, so it's safe to call on either.
        const title = rawTitle ? this.#localize.string(rawTitle) : undefined;
        const message = rawMessage ? this.#localize.string(rawMessage) : undefined;
        const approveLabel = this.#localize.string(
            (this.config.approveLabel as string) ??
                (this.args.approveLabel as string) ??
                "#uaiChat_approvalApprove",
        );
        const denyLabel = this.#localize.string(
            (this.config.denyLabel as string) ?? (this.args.denyLabel as string) ?? "#uaiChat_approvalDeny",
        );

        return html`
            ${title ? html`<div class="title">${title}</div>` : ""}
            ${message ? html`<div class="message">${message}</div>` : ""}
            <div class="actions ${title || message ? "" : "no-content-above"}">
                <uui-button look="primary" color="positive" @click=${this.#handleApprove}> ${approveLabel} </uui-button>
                <uui-button look="primary" @click=${this.#handleDeny}> ${denyLabel} </uui-button>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        .title {
            font-weight: bold;
            margin-bottom: var(--uui-size-space-1);
        }

        .message {
            color: var(--uui-color-text-alt);
        }

        .actions {
            display: flex;
            gap: var(--uui-size-space-2);
            /* On the button row's own margin (not the title/message's) so the gap above the buttons
               is consistent whether there's a title only, a message only, or both. Omitted entirely
               (see .no-content-above) when neither is present, so the card doesn't reserve empty space. */
            margin-top: var(--uui-size-space-4);
        }

        .actions.no-content-above {
            margin-top: 0;
        }
    `;
}

export default UaiAgentApprovalDefaultElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-approval-default": UaiAgentApprovalDefaultElement;
    }
}
