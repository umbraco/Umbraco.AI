import { customElement, html, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    UaiProjectPickerModalData,
    UaiProjectPickerModalItem,
    UaiProjectPickerModalValue,
} from "./project-picker-modal.token.js";

/**
 * Centered project picker for "New chat in a project", styled like the CMS create modal
 * (`uui-dialog-layout` + `uui-ref-node` rows with icon / name / description + Cancel). Selecting a
 * project resolves the modal with its id.
 */
@customElement("uai-project-picker-modal")
export class UaiProjectPickerModalElement extends UmbModalBaseElement<
    UaiProjectPickerModalData,
    UaiProjectPickerModalValue
> {
    #select(project: UaiProjectPickerModalItem) {
        this.value = { projectId: project.id };
        this._submitModal();
    }

    override render() {
        const projects = this.data?.projects ?? [];
        return html`
            <uui-dialog-layout headline=${this.localize.term("uaiCopilotWorkspace_newChatInProject")}>
                <uui-ref-list>
                ${repeat(
                    projects,
                    (p) => p.id,
                    (p) => html`
                        <uui-ref-node
                            name=${p.name}
                            detail=${p.description ?? ""}
                            select-only
                            selectable
                            @selected=${() => this.#select(p)}
                            @open=${() => this.#select(p)}
                        >
                            <uui-icon slot="icon" name="icon-folder"></uui-icon>
                        </uui-ref-node>
                    `,
                )}
                </uui-ref-list>
                <uui-button
                    slot="actions"
                    id="cancel"
                    label=${this.localize.term("general_cancel")}
                    @click=${this._rejectModal}
                ></uui-button>
            </uui-dialog-layout>
        `;
    }

    static override styles = [UmbTextStyles];
}

export default UaiProjectPickerModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-project-picker-modal": UaiProjectPickerModalElement;
    }
}
