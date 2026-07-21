import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiConversationServerDataSource } from "./conversation.server.data-source.js";
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
        return this.#source.create(request);
    }

    async delete(id: string) {
        return this.#source.delete(id);
    }

    async update(id: string, request: UpdateConversationRequestModel) {
        return this.#source.update(id, request);
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
}

/** Projects the full mutable surface of a conversation into an update request. */
function toUpdateModel(conversation: ConversationResponseModel): UpdateConversationRequestModel {
    return {
        title: conversation.title ?? null,
        projectId: conversation.projectId ?? null,
        agentIdOrAlias: conversation.agentIdOrAlias ?? null,
        profileId: conversation.profileId ?? null,
        isPinned: conversation.isPinned,
        isArchived: conversation.isArchived,
    };
}
