import { html, customElement, css, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { umbFocus } from "@umbraco-cms/backoffice/lit-element";
import type {
    UaiRenameConversationModalData,
    UaiRenameConversationModalValue,
} from "./rename-conversation-modal.token.js";

/** Small single-input modal for renaming a conversation (opened by the Rename entity action). */
@customElement("uai-copilot-workspace-rename-conversation-modal")
export class UaiRenameConversationModalElement extends UmbModalBaseElement<
    UaiRenameConversationModalData,
    UaiRenameConversationModalValue
> {
    @state()
    private _name = "";

    override connectedCallback(): void {
        super.connectedCallback();
        this._name = this.data?.value ?? "";
    }

    #onSubmit(event: SubmitEvent) {
        event.preventDefault();
        const form = event.target as HTMLFormElement;
        if (!form.checkValidity()) return;
        const name = ((new FormData(form).get("name") as string) ?? "").trim();
        if (!name) return;
        this.value = { name };
        this._submitModal();
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.localize.term("uaiCopilotWorkspace_renamePrompt")}>
                <uui-box>
                    <uui-form>
                        <form id="RenameForm" @submit=${this.#onSubmit}>
                            <uui-form-layout-item>
                                <uui-label id="nameLabel" for="name" slot="label" required>
                                    ${this.localize.term("uaiCopilotWorkspace_projectNameLabel")}
                                </uui-label>
                                <uui-input
                                    id="name"
                                    name="name"
                                    .value=${this._name}
                                    required
                                    required-message="Required"
                                    ${umbFocus()}
                                ></uui-input>
                            </uui-form-layout-item>
                        </form>
                    </uui-form>
                </uui-box>
                <uui-button
                    slot="actions"
                    id="cancel"
                    label=${this.localize.term("general_cancel")}
                    @click=${this._rejectModal}
                ></uui-button>
                <uui-button
                    slot="actions"
                    form="RenameForm"
                    type="submit"
                    color="positive"
                    look="primary"
                    label=${this.localize.term("general_submit")}
                ></uui-button>
            </umb-body-layout>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            #name {
                width: 100%;
            }
        `,
    ];
}

export default UaiRenameConversationModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-rename-conversation-modal": UaiRenameConversationModalElement;
    }
}
