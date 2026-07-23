import { customElement, html, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type { UaiProjectPickerModalData, UaiProjectPickerModalValue } from "./project-picker-modal.token.js";

/**
 * Centered project picker styled like the CMS create modal (`uui-dialog-layout` + `uui-ref-node` rows
 * with icon / name / description + Cancel). Shared by "New chat in a project" and the Move-to-project
 * action; when `data.noProjectLabel` is set, a leading "no project" row resolves `{ projectId: null }`.
 */
@customElement("uai-copilot-workspace-project-picker-modal")
export class UaiProjectPickerModalElement extends UmbModalBaseElement<
    UaiProjectPickerModalData,
    UaiProjectPickerModalValue
> {
    #select(projectId: string | null) {
        this.value = { projectId };
        this._submitModal();
    }

    override render() {
        const projects = this.data?.projects ?? [];
        const headline = this.data?.headline ?? this.localize.term("uaiCopilotWorkspace_newChatInProject");
        const noProjectLabel = this.data?.noProjectLabel;
        return html`
            <uui-dialog-layout headline=${headline}>
                <uui-ref-list>
                    ${noProjectLabel
                        ? html`
                              <uui-ref-node
                                  name=${noProjectLabel}
                                  select-only
                                  selectable
                                  @selected=${() => this.#select(null)}
                                  @open=${() => this.#select(null)}
                              >
                                  <uui-icon slot="icon" name="icon-delete"></uui-icon>
                              </uui-ref-node>
                          `
                        : nothing}
                    ${repeat(
                        projects,
                        (p) => p.id,
                        (p) => html`
                            <uui-ref-node
                                name=${p.name}
                                detail=${p.description ?? ""}
                                select-only
                                selectable
                                @selected=${() => this.#select(p.id)}
                                @open=${() => this.#select(p.id)}
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
        "uai-copilot-workspace-project-picker-modal": UaiProjectPickerModalElement;
    }
}
