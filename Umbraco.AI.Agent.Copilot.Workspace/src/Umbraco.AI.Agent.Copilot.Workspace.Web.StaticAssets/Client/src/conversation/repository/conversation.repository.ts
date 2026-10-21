import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UaiConversationServerDataSource } from "./conversation.server.data-source.js";
import { UAI_CONVERSATION_ENTITY_TYPE } from "../../constants.js";
import { toConversationDetailModel, toUpdateConversationRequestModel } from "../types.js";
import type {
    ConversationResponseModel,
    CreateConversationRequestModel,
    ShareConversationRequestModel,
    UaiConversationCollectionFilter,
    UpdateConversationRequestModel,
} from "../types.js";

/**
 * Repository for conversation collection + mutation operations used by the
 * Copilot Workspace section sidebar. The management API's update endpoint takes
 * the full conversation state, so the single-field helpers (`setPinned`, etc.)
 * rebuild the whole {@link UpdateConversationRequestModel} from a known-current
 * entity to avoid clobbering unrelated fields.
 */
export class UaiConversationRepository extends UmbRepositoryBase {
    #source: UaiConversationServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#source = new UaiConversationServerDataSource(host);
    }

    async requestCollection(filter: UaiConversationCollectionFilter = {}) {
        return this.#source.getCollection(filter);
    }

    async create(request: CreateConversationRequestModel) {
        const result = await this.#source.create(request);
        if (!result.error && result.data?.id) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    async delete(id: string) {
        const result = await this.#source.delete(id);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    async update(id: string, request: UpdateConversationRequestModel) {
        const result = await this.#source.update(id, request);
        if (!result.error) {
            // Awaited, unlike the create/delete dispatches: the workspace store releases its self-write
            // guard when this method settles, and the dispatch resolves a context first, so leaving it
            // unawaited lands the event after the guard is gone and the store refetches its own write.
            await dispatchActionEvent(this, UaiEntityActionEvent.updated(id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    async requestMessages(id: string) {
        return this.#source.getMessages(id);
    }

    /**
     * Drops the stored answer to the conversation's last user message so it can be regenerated. No entity
     * action event: only the message thread changes, and the chat that asked for this already reflects it.
     */
    async truncateAfterLastUserMessage(id: string) {
        return this.#source.truncateAfterLastUserMessage(id);
    }

    async requestById(id: string) {
        return this.#source.getById(id);
    }

    setPinned(conversation: ConversationResponseModel, isPinned: boolean) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), isPinned });
    }

    setArchived(conversation: ConversationResponseModel, isArchived: boolean) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), isArchived });
    }

    rename(conversation: ConversationResponseModel, title: string) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), title });
    }

    moveToProject(conversation: ConversationResponseModel, projectId: string | null) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), projectId });
    }

    /**
     * Shares (or re-shares) the conversation with one recipient. The wire request still carries
     * `recipientUserKeys` as an array — kept for a future bulk entry point, see SPEC.md — but this
     * method only ever sends the one recipient passed in.
     */
    async share(
        conversation: ConversationResponseModel,
        recipientUserKey: string,
        scopeMode: ShareConversationRequestModel["scopeMode"],
    ) {
        const result = await this.#source.share(conversation.id, {
            recipientUserKeys: [recipientUserKey],
            scopeMode,
        });
        if (!result.error) {
            // Dispatched (and awaited, same timing reason as `update`) because sharing flips the
            // owner's `hasActiveShares` badge on the sidebar's conversation list, which otherwise
            // has no way to know this conversation's share state just changed.
            await dispatchActionEvent(this, UaiEntityActionEvent.updated(conversation.id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    getShares(conversation: ConversationResponseModel) {
        return this.#source.getShares(conversation.id);
    }

    async revokeShare(conversation: ConversationResponseModel, userKey: string) {
        const result = await this.#source.revokeShare(conversation.id, userKey);
        if (!result.error) {
            // Dispatched (and awaited, same timing reason as `update`) because revoking the last share
            // flips the owner's `hasActiveShares` badge back off, which otherwise has no way to know
            // this conversation's share state just changed.
            await dispatchActionEvent(this, UaiEntityActionEvent.updated(conversation.id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    requestSharedWithMe(skip?: number, take?: number, search?: string) {
        return this.#source.getSharedWithMe(skip, take, search);
    }

    requestSharedView(id: string) {
        return this.#source.getSharedView(id);
    }
}

/**
 * Projects the full mutable surface of a conversation into an update request. Agent, context and resource
 * edits don't go through here — the workspace store owns those and writes the whole detail model, so it
 * can buffer them while the conversation is still an unsaved draft.
 */
function toUpdateModel(conversation: ConversationResponseModel): UpdateConversationRequestModel {
    return toUpdateConversationRequestModel(toConversationDetailModel(conversation));
}
