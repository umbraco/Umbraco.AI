import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for AI Traces with search and filter toolbar.
 */
@customElement("uai-trace-collection")
export class UaiTraceCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiTraceCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-trace-collection": UaiTraceCollectionElement;
    }
}
