import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Prompts with search header.
 */
@customElement("uai-prompt-collection")
export class UaiPromptCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiPromptCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-collection": UaiPromptCollectionElement;
    }
}
