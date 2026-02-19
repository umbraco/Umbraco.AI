import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN } from "../../workspace/paths.js";
import { UAI_TEST_CREATE_OPTIONS_MODAL } from "../../modals/create-options/test-create-options-modal.token.js";

/**
 * Collection action element for creating a new test.
 * Opens the test feature selection modal before navigating to the create workspace.
 */
@customElement("uai-test-create-collection-action")
export class UaiTestCreateCollectionActionElement extends UmbLitElement {
    @state()
    private _loading = false;

    async #onCreate() {
        if (this._loading) return;
        this._loading = true;

        try {
            const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
            if (!modalManager) return;

            const result = await modalManager
                .open(this, UAI_TEST_CREATE_OPTIONS_MODAL, {
                    data: { headline: "Select Test Feature" },
                })
                .onSubmit()
                .catch(() => undefined);

            if (!result?.testFeatureId) return;

            const path = UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN.generateAbsolute({
                testFeatureId: result.testFeatureId,
            });

            history.pushState(null, "", path);
        } finally {
            this._loading = false;
        }
    }

    override render() {
        return html`
            <uui-button look="outline" @click=${this.#onCreate} .disabled=${this._loading}>
                ${this._loading ? "Loading..." : "Create"}
            </uui-button>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                align-items: center;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-create-collection-action": UaiTestCreateCollectionActionElement;
    }
}

export default UaiTestCreateCollectionActionElement;
