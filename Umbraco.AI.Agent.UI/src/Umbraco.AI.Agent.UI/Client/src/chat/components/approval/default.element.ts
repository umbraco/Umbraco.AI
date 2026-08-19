import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
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

    /**
     * What the approver has typed so far into the confirmation box, when {@link confirmPhrase} gates
     * the Approve button. Reset implicitly since this element is recreated per interrupt.
     */
    @state()
    private _typedConfirmation = "";

    #handleApprove() {
        this.respond({ approved: true });
    }

    #handleDeny() {
        this.respond({ approved: false });
    }

    #handleConfirmInput(event: UUIInputEvent) {
        const target = event.composedPath()[0] as UUIInputElement;
        this._typedConfirmation = target.value?.toString() ?? "";
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
        // Set only for calls that warrant more friction than a plain click (e.g. publish/delete) --
        // a literal string (typically the target item's name), never a localization key.
        const confirmPhrase = (this.config.confirmPhrase as string) ?? (this.args.confirmPhrase as string);
        const approveLabel = this.#localize.string(
            (this.config.approveLabel as string) ??
                (this.args.approveLabel as string) ??
                "#uaiChat_approvalApprove",
        );
        const denyLabel = this.#localize.string(
            (this.config.denyLabel as string) ?? (this.args.denyLabel as string) ?? "#uaiChat_approvalDeny",
        );
        const approveDisabled = !!confirmPhrase && this._typedConfirmation !== confirmPhrase;

        return html`
            ${title ? html`<div class="title">${title}</div>` : ""}
            ${message ? html`<div class="message">${message}</div>` : ""}
            ${confirmPhrase
                ? html`
                      <div class="confirm-phrase">
                          <label for="confirm-input"
                              >${this.#localize.htmlString("#uaiChat_approvalConfirmPhraseLabel", confirmPhrase)}</label
                          >
                          <uui-input
                              id="confirm-input"
                              label=${this.#localize.term("uaiChat_approvalConfirmPhraseLabelPlain", confirmPhrase)}
                              .value=${this._typedConfirmation}
                              @input=${this.#handleConfirmInput}
                          ></uui-input>
                      </div>
                  `
                : ""}
            <div class="actions ${title || message || confirmPhrase ? "" : "no-content-above"}">
                <uui-button
                    look="primary"
                    color="positive"
                    ?disabled=${approveDisabled}
                    @click=${this.#handleApprove}
                >
                    ${approveLabel}
                </uui-button>
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

        .confirm-phrase {
            margin-top: var(--uui-size-space-4);
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-1);
        }

        .confirm-phrase label {
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .confirm-phrase uui-input {
            width: 100%;
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
