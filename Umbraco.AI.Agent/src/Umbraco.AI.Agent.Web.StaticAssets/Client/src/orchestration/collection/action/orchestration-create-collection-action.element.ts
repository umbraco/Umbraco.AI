import { css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_CREATE_ORCHESTRATION_WORKSPACE_PATH_PATTERN } from "../../workspace/orchestration/paths.js";

/**
 * Collection action button for creating a new orchestration.
 */
@customElement("uai-orchestration-create-collection-action")
export class UaiOrchestrationCreateCollectionActionElement extends UmbLitElement {
    #onClick() {
        const path = UAI_CREATE_ORCHESTRATION_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }

    render() {
        return html`
            <uui-button color="default" look="outline" label="Create" @click=${this.#onClick}> Create </uui-button>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                height: 100%;
                display: flex;
                align-items: center;
            }
        `,
    ];
}

export default UaiOrchestrationCreateCollectionActionElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-create-collection-action": UaiOrchestrationCreateCollectionActionElement;
    }
}
