import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Contexts with search header.
 */
@customElement("uai-context-collection")
export class UaiContextCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiContextCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-context-collection": UaiContextCollectionElement;
    }
}
