import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Tests with search header.
 */
@customElement("uai-test-collection")
export class UaiTestCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiTestCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-collection": UaiTestCollectionElement;
    }
}
