import { html, customElement, css } from "@umbraco-cms/backoffice/external/lit";
import { UMB_COLLECTION_CONTEXT, UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";

/**
 * Collection element for Test Runs with search header and polling.
 */
@customElement("uai-test-run-collection")
export class UaiTestRunCollectionElement extends UmbCollectionDefaultElement {
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

export { UaiTestRunCollectionElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-collection": UaiTestRunCollectionElement;
    }
}
