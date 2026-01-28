import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { customElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiRollbackModalData, UaiRollbackModalValue } from "./rollback-modal.token.js";
import "../../components/version-diff-view/version-diff-view.element.js";

/**
 * A modal component for confirming a rollback to a previous version.
 * Displays a diff view of the changes and allows the user to confirm or cancel.
 *
 * @element uai-rollback-modal
 */
@customElement("uai-rollback-modal")
export class UaiRollbackModalElement extends UmbModalBaseElement<
    UaiRollbackModalData,
    UaiRollbackModalValue
> {
    #onRollback() {
        this.updateValue({ rollback: true });
        this._submitModal();
    }

    override render() {
        if (!this.data) return html``;
        
        const headline = this.localize.term("uaiVersionHistory_compareVersions", 
            this.data.fromVersion,
            this.data.toVersion);

        return html`
            <umb-body-layout headline=${headline}>
                <div id="main">
                    <uui-box headline=${this.localize.term("uaiVersionHistory_changes")}>
                        <uai-version-diff-view
                            .changes=${this.data.changes}>
                        </uai-version-diff-view>
                    </uui-box>
                </div>

                <uui-button
                    slot="actions"
                    id="close"
                    label=${this.localize.term("general_close")}
                    @click=${this._rejectModal}>
                    ${this.localize.term("general_close")}
                </uui-button>
                <uui-button
                    slot="actions"
                    id="rollback"
                    color="positive"
                    look="primary"
                    label=${this.localize.term("uaiVersionHistory_rollback", [this.data.fromVersion])}
                    @click=${this.#onRollback}>
                    ${this.localize.term("uaiVersionHistory_rollback", [this.data.fromVersion])}
                </uui-button>
            </umb-body-layout>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                height: 100%;
            }

            #main {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            .description p {
                margin: 0;
            }
        `,
    ];
}

export default UaiRollbackModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-rollback-modal": UaiRollbackModalElement;
    }
}
