import { css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN } from "../../workspace/prompt/paths.js";

/**
 * Collection action button for creating a new prompt.
 */
@customElement("uai-prompt-create-collection-action")
export class UaiPromptCreateCollectionActionElement extends UmbLitElement {
    #onClick() {
        const path = UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }

    render() {
        return html`
            <uui-button
                color="default"
                look="outline"
                label="Create Prompt"
                @click=${this.#onClick}
            >
                Create Prompt
            </uui-button>
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

export default UaiPromptCreateCollectionActionElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-create-collection-action": UaiPromptCreateCollectionActionElement;
    }
}
