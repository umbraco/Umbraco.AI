import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UaiConversationServerDataSource } from "./conversation.server.data-source.js";
import { UAI_CONVERSATION_ENTITY_TYPE } from "../../constants.js";
import type { ContextResourceModel } from "../../api/types.gen.js";
import type {
    ConversationResponseModel,
    CreateConversationRequestModel,
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
            dispatchActionEvent(this, UaiEntityActionEvent.updated(id, UAI_CONVERSATION_ENTITY_TYPE));
        }
        return result;
    }

    async requestMessages(id: string) {
        return this.#source.getMessages(id);
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

    /** Sets the conversation's chosen agent (id/alias, or "auto"); the server resolves it on the next turn. */
    setAgentIdOrAlias(conversation: ConversationResponseModel, agentIdOrAlias: string) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), agentIdOrAlias });
    }

    /** Sets the conversation's own attached context ids (stacked on top of its project's). */
    setContextIds(conversation: ConversationResponseModel, contextIds: string[]) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), contextIds });
    }

    /** Sets the conversation's own attached resources (stacked on top of its project's). */
    setResources(conversation: ConversationResponseModel, resources: ContextResourceModel[]) {
        return this.update(conversation.id, { ...toUpdateModel(conversation), resources });
    }
}

/** Projects the full mutable surface of a conversation into an update request. */
function toUpdateModel(conversation: ConversationResponseModel): UpdateConversationRequestModel {
    return {
        title: conversation.title ?? null,
        projectId: conversation.projectId ?? null,
        agentIdOrAlias: conversation.agentIdOrAlias ?? null,
        profileId: conversation.profileId ?? null,
        contextIds: [...conversation.contextIds],
        resources: [...conversation.resources],
        isPinned: conversation.isPinned,
        isArchived: conversation.isArchived,
    };
}
