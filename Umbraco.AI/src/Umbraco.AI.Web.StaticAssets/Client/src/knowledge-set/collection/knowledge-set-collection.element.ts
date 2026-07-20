import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Knowledge Sets with search header.
 *
 * Read-only mirror of the Context collection element: the default toolbar is replaced with one that
 * only contains the filter field (no create action).
 */
@customElement("uai-knowledge-set-collection")
export class UaiKnowledgeSetCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiKnowledgeSetCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-collection": UaiKnowledgeSetCollectionElement;
    }
}
