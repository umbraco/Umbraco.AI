import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbCollectionWorkspaceViewElement } from "@umbraco-cms/backoffice/collection";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";

/**
 * Workspace view for test runs scoped to a specific test.
 * Follows the webhook delivery pattern: extends UmbCollectionWorkspaceViewElement
 * and sets a filter to scope the collection to the current test entity.
 */
@customElement("uai-test-runs-workspace-view")
export class UaiTestRunsWorkspaceViewElement extends UmbCollectionWorkspaceViewElement {
    constructor() {
        super();
        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (instance) => {
            this.observe(instance?.unique, (unique) => this.#setTestFilter(unique));
        });
    }

    #setTestFilter(unique: string | undefined) {
        if (unique === undefined) {
            this._filter = undefined;
        } else {
            this._filter = {
                test: unique ? { unique } : null,
            };
        }
    }
}

export { UaiTestRunsWorkspaceViewElement as element };

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-runs-workspace-view": UaiTestRunsWorkspaceViewElement;
    }
}
