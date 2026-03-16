import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Guardrails with search header.
 */
@customElement("uai-guardrail-collection")
export class UaiGuardrailCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiGuardrailCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-guardrail-collection": UaiGuardrailCollectionElement;
    }
}
