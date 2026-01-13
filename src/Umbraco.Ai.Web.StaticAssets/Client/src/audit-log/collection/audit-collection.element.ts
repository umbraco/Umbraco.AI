import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for AI AuditLog Logs with search and filter toolbar.
 */
@customElement("uai-audit-collection")
export class UaiAuditLogCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiAuditLogCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-audit-collection": UaiAuditLogCollectionElement;
    }
}
