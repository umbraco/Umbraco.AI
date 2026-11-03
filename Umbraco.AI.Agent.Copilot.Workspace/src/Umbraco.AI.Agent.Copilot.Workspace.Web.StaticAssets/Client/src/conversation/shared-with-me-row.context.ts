import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

/**
 * Per-row context carrying the current "shared with me" row's owner display name. Provided by each
 * `uai-copilot-workspace-shared-with-me-tree-item` so the real `entitySign` extension system (its sign
 * api, constructed as a descendant of the row inside `<umb-entity-sign-bundle>`) can read a per-row
 * value and expose it as a reactive `label` observable — mirroring `UaiConversationEntityContext`'s
 * per-node pattern for the owner-side tree item.
 */
export class UaiSharedConversationRowContext extends UmbContextBase {
    #ownerDisplayName = new UmbStringState<string | undefined>(undefined);

    readonly ownerDisplayName = this.#ownerDisplayName.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UAI_SHARED_CONVERSATION_ROW_CONTEXT);
    }

    setOwnerDisplayName(value: string | undefined) {
        this.#ownerDisplayName.setValue(value);
    }

    getOwnerDisplayName(): string | undefined {
        return this.#ownerDisplayName.getValue();
    }
}

export const UAI_SHARED_CONVERSATION_ROW_CONTEXT = new UmbContextToken<UaiSharedConversationRowContext>(
    "UaiSharedConversationRowContext",
);
