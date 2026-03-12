import { html, customElement, css } from "@umbraco-cms/backoffice/external/lit";
import { UMB_COLLECTION_CONTEXT, UmbCollectionDefaultElement } from "@umbraco-cms/backoffice/collection";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../workspace/test/test-workspace.context-token.js";

/**
 * Collection element for Test Runs with search header and polling.
 */
@customElement("uai-test-run-collection")
export class UaiTestRunCollectionElement extends UmbCollectionDefaultElement {
    constructor() {
        super();
        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.observe(context.testRunVersion, (version) => {
                if (version > 0) {
                    this.#reloadCollection();
                }
            });
        });
    }

    async #reloadCollection() {
        const ctx = await this.getContext(UMB_COLLECTION_CONTEXT);
        if (ctx) {
            ctx.loadCollection();
        }
    }

    async #onPollInterval() {
        await this.#reloadCollection();
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
