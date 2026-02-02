import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN } from "../../workspace/context/paths.js";

/**
 * Collection action element for creating a new context.
 * Navigates directly to the create workspace.
 */
@customElement("uai-context-create-collection-action")
export class UaiContextCreateCollectionActionElement extends UmbLitElement {
    #onCreate() {
        const path = UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }

    override render() {
        return html`
            <uui-button look="outline" @click=${this.#onCreate}>
                Create Context
            </uui-button>
        `;
    }
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-context-create-collection-action": UaiContextCreateCollectionActionElement;
    }
}

export default UaiContextCreateCollectionActionElement;
