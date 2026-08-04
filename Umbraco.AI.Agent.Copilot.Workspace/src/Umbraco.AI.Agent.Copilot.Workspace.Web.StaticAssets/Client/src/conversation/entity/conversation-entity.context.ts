import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { ConversationResponseModel } from "../types.js";

/**
 * Per-node context carrying a conversation's current model. Provided by each conversation tree item
 * so the standard entity-action system can resolve the item, and so the pin/archive state conditions
 * can gate their action pairs (Pin↔Unpin, Archive↔Unarchive) from the live state — mirroring the CMS
 * `UmbIsTrashedEntityContext` pattern used for trash/restore.
 */
export class UaiConversationEntityContext extends UmbContextBase {
    #model = new UmbObjectState<ConversationResponseModel | undefined>(undefined);

    readonly model = this.#model.asObservable();
    readonly isPinned = this.#model.asObservablePart((c) => c?.isPinned ?? false);
    readonly isArchived = this.#model.asObservablePart((c) => c?.isArchived ?? false);

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONVERSATION_ENTITY_CONTEXT);
    }

    setModel(model: ConversationResponseModel | undefined) {
        this.#model.setValue(model);
    }

    getModel(): ConversationResponseModel | undefined {
        return this.#model.getValue();
    }
}

export const UAI_CONVERSATION_ENTITY_CONTEXT = new UmbContextToken<UaiConversationEntityContext>(
    "UaiConversationEntityContext",
);
