import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Agents with search header.
 */
@customElement("uai-prompt-collection")
export class UAiAgentCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UAiAgentCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-collection": UAiAgentCollectionElement;
    }
}
