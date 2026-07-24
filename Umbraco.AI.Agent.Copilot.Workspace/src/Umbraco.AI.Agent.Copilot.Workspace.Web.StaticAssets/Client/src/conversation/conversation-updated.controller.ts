import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent } from "@umbraco-ai/core";
import { UAI_CONVERSATION_ENTITY_TYPE } from "../constants.js";

/**
 * Invokes {@link onUpdated} whenever the conversation identified by {@link getConversationId} is updated
 * elsewhere on the shared action-event bus — e.g. archived/unarchived from its sidebar ⋯ menu — so a view
 * showing that conversation can react in place. Owns the listener lifecycle: it is removed when the host
 * controller is destroyed (these controllers are recreated per navigation, so leaving the listener
 * attached would leak a handler bound to a dead view on every switch).
 *
 * Shared by the center chat context and the right-region context panel, which live in separate subtrees
 * and so each need their own subscription to the same open conversation.
 */
export class UaiConversationUpdatedController extends UmbControllerBase {
    #actionContext?: EventTarget;
    readonly #getConversationId: () => string | undefined;
    readonly #onUpdated: () => void;

    constructor(host: UmbControllerHost, getConversationId: () => string | undefined, onUpdated: () => void) {
        super(host);
        this.#getConversationId = getConversationId;
        this.#onUpdated = onUpdated;

        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            this.#actionContext?.removeEventListener(UaiEntityActionEvent.UPDATED, this.#handle as EventListener);
            this.#actionContext = context ?? undefined;
            context?.addEventListener(UaiEntityActionEvent.UPDATED, this.#handle as EventListener);
        });
    }

    #handle = (event: UaiEntityActionEvent) => {
        if (event.getEntityType() !== UAI_CONVERSATION_ENTITY_TYPE) return;
        const id = this.#getConversationId();
        if (!id || event.getUnique() !== id) return;
        this.#onUpdated();
    };

    override destroy(): void {
        this.#actionContext?.removeEventListener(UaiEntityActionEvent.UPDATED, this.#handle as EventListener);
        super.destroy();
    }
}
