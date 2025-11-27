import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Connections with search header.
 */
@customElement("uai-connection-collection")
export class UaiConnectionCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiConnectionCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-collection": UaiConnectionCollectionElement;
    }
}
