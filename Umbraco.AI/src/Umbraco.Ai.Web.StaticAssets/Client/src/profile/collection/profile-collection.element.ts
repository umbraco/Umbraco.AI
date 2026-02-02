import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Custom collection element for Profiles with search header.
 */
@customElement("uai-profile-collection")
export class UaiProfileCollectionElement extends UmbCollectionDefaultElement {
    protected override renderToolbar() {
        return html`
            <umb-collection-toolbar slot="header">
                <umb-collection-filter-field></umb-collection-filter-field>
            </umb-collection-toolbar>
        `;
    }
}

export { UaiProfileCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-collection": UaiProfileCollectionElement;
    }
}
