import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Orchestrations with search header.
 */
@customElement("uai-orchestration-collection")
export class UaiOrchestrationCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiOrchestrationCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-collection": UaiOrchestrationCollectionElement;
    }
}
