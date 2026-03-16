import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_CREATE_GUARDRAIL_WORKSPACE_PATH_PATTERN } from "../../workspace/guardrail/paths.js";

/**
 * Collection action element for creating a new guardrail.
 * Navigates directly to the create workspace.
 */
@customElement("uai-guardrail-create-collection-action")
export class UaiGuardrailCreateCollectionActionElement extends UmbLitElement {
    #onCreate() {
        const path = UAI_CREATE_GUARDRAIL_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }

    override render() {
        return html` <uui-button look="outline" @click=${this.#onCreate}> Create </uui-button> `;
    }
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-guardrail-create-collection-action": UaiGuardrailCreateCollectionActionElement;
    }
}

export default UaiGuardrailCreateCollectionActionElement;
