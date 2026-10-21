import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbUserPickerInputContext, type UmbUserItemModel } from "@umbraco-cms/backoffice/user";
import { UAI_DEFAULT_SHARE_SCOPE_MODE, uaiBuildShareValue, uaiCanSubmitShare } from "./share-picker-modal.helpers.js";
import type {
    UaiConversationShareScopeMode,
    UaiSharePickerModalData,
    UaiSharePickerModalValue,
} from "./share-picker-modal.token.js";

/**
 * Single-recipient share form: a one-item user-picker field (built on `UmbUserPickerInputContext`,
 * the same picker context a user-picker property editor uses — `max = 1` makes it open
 * `UMB_USER_PICKER_MODAL` with `multiple: false`, per ARCHITECTURE.md decision 7), a Snapshot/Live
 * `uui-select`, and a disclosure banner shown only when the conversation has attachments. Resolves
 * `{ recipientUserKey, scopeMode }` on submit — it does not call `UaiConversationRepository.share()`
 * itself; its caller (the "Shared with" section's "Choose" button, SC-14) does that after the modal
 * resolves, same split as `UAI_RENAME_CONVERSATION_MODAL`.
 */
@customElement("uai-copilot-workspace-share-picker-modal")
export class UaiSharePickerModalElement extends UmbModalBaseElement<UaiSharePickerModalData, UaiSharePickerModalValue> {
    #picker = new UmbUserPickerInputContext(this);

    @state()
    private _recipient?: UmbUserItemModel;

    @state()
    private _scopeMode: UaiConversationShareScopeMode = UAI_DEFAULT_SHARE_SCOPE_MODE;

    constructor() {
        super();
        this.#picker.max = 1;
        this.observe(this.#picker.selectedItems, (items) => (this._recipient = items[0]), "_observeRecipient");
    }

    #openPicker() {
        this.#picker.openPicker();
    }

    #onModeChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const select = event.target as HTMLElement & { value: string };
        this._scopeMode = select.value as UaiConversationShareScopeMode;
    }

    #onSubmit() {
        if (!this._recipient) return;
        this.value = uaiBuildShareValue(this._recipient.unique, this._scopeMode);
        this._submitModal();
    }

    override render() {
        const headline = this.localize.term("uaiCopilotWorkspace_sharePickerHeadline", this.data?.title ?? "");
        return html`
            <umb-body-layout headline=${headline}>
                <uui-box>
                    <umb-property-layout
                        orientation="vertical"
                        label=${this.localize.term("uaiCopilotWorkspace_sharePersonLabel")}
                        description=${this.localize.term("uaiCopilotWorkspace_sharePersonHelp")}
                    >
                        <div slot="editor">
                            ${this._recipient
                                ? html`
                                      <uui-ref-node-user
                                          name=${this._recipient.name}
                                          detail=${this.localize.term("uaiCopilotWorkspace_sharePickDifferentPerson")}
                                          @open=${this.#openPicker}
                                      ></uui-ref-node-user>
                                  `
                                : html`
                                      <uui-button
                                          id="choose-person"
                                          look="placeholder"
                                          label=${this.localize.term("uaiCopilotWorkspace_sharePersonChoose")}
                                          @click=${this.#openPicker}
                                      ></uui-button>
                                  `}
                        </div>
                    </umb-property-layout>

                    <umb-property-layout
                        orientation="vertical"
                        label=${this.localize.term("uaiCopilotWorkspace_shareModeLabel")}
                    >
                        <div slot="editor">
                            <uui-select
                                label=${this.localize.term("uaiCopilotWorkspace_shareModeLabel")}
                                .value=${this._scopeMode}
                                .options=${[
                                    {
                                        name: this.localize.term("uaiCopilotWorkspace_shareModeSnapshot"),
                                        value: "Snapshot",
                                        selected: this._scopeMode === "Snapshot",
                                    },
                                    {
                                        name: this.localize.term("uaiCopilotWorkspace_shareModeLive"),
                                        value: "Live",
                                        selected: this._scopeMode === "Live",
                                    },
                                ]}
                                @change=${this.#onModeChange}
                            ></uui-select>
                        </div>
                    </umb-property-layout>

                    ${this.data?.hasAttachments
                        ? html`
                              <div class="disclosure-banner">
                                  <uui-icon name="icon-alert"></uui-icon>
                                  <span>${this.localize.term("uaiCopilotWorkspace_shareDisclosureBanner")}</span>
                              </div>
                          `
                        : nothing}
                </uui-box>

                <uui-button
                    slot="actions"
                    id="cancel"
                    label=${this.localize.term("general_cancel")}
                    @click=${this._rejectModal}
                ></uui-button>
                <uui-button
                    slot="actions"
                    id="submit"
                    color="positive"
                    look="primary"
                    label=${this.localize.term("uaiCopilotWorkspace_shareSubmit")}
                    ?disabled=${!uaiCanSubmitShare(this._recipient?.unique)}
                    @click=${this.#onSubmit}
                ></uui-button>
            </umb-body-layout>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            #choose-person {
                display: block;
            }

            uui-select {
                width: 100%;
            }

            .disclosure-banner {
                display: flex;
                align-items: flex-start;
                gap: var(--uui-size-space-3);
                margin-top: var(--uui-size-space-4);
                padding: var(--uui-size-space-4);
                background: var(--uui-color-warning);
                color: var(--uui-color-warning-contrast);
                border-radius: var(--uui-border-radius);
            }

            .disclosure-banner uui-icon {
                flex-shrink: 0;
            }
        `,
    ];
}

export default UaiSharePickerModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-share-picker-modal": UaiSharePickerModalElement;
    }
}
