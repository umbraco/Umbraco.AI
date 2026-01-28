import { html, customElement, css } from "@umbraco-cms/backoffice/external/lit";
import { UMB_COLLECTION_CONTEXT, UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for AI AuditLog Logs with search and filter toolbar.
 */
@customElement("uai-audit-collection")
export class UaiAuditLogCollectionElement extends UmbCollectionDefaultElement {
    
    async #onPollInterval() {
        const ctx = await this.getContext(UMB_COLLECTION_CONTEXT);
        if (ctx) {
            ctx.loadCollection();
        }
    }
    
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <div class="header-contents">
                    <umb-collection-filter-field></umb-collection-filter-field>
                    <uai-polling-button @interval=${this.#onPollInterval}></uai-polling-button>
                </div>
            </umb-collection-toolbar>
        `;
    }

    static styles = [
        css`
			.header-contents {
                display: flex;
                gap: 1rem;
                align-items: center;
            }

            umb-collection-filter-field {
                flex-grow: 1;
            }
		`,
    ];
}

export { UaiAuditLogCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-audit-collection": UaiAuditLogCollectionElement;
    }
}
