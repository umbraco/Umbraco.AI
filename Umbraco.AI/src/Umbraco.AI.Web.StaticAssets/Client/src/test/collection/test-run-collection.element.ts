import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Collection element for Test Runs with search header.
 */
@customElement("uai-test-run-collection")
export class UaiTestRunCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiTestRunCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-collection": UaiTestRunCollectionElement;
    }
}
