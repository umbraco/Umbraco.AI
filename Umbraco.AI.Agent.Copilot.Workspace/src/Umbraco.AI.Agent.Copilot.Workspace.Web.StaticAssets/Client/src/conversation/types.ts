import type {
    ContextResourceModel,
    ConversationResponseModel,
    CreateConversationRequestModel,
    UpdateConversationRequestModel,
} from "../api/types.gen.js";

export type {
    ContextResourceModel,
    ConversationResponseModel,
    CreateConversationRequestModel,
    UpdateConversationRequestModel,
};

/** Filter for listing conversations (mirrors the management API query). */
export interface UaiConversationCollectionFilter {
    projectId?: string;
    search?: string;
    includeArchived?: boolean;
    skip?: number;
    take?: number;
}

/**
 * The workspace-editing shape for a conversation — the one model the workspace store holds, whether the
 * conversation is persisted or still an unsaved draft. `id` is the discriminator: undefined means nothing
 * has been written to the server yet, so edits are buffered until `commitDraft` sends them all in the
 * single create request.
 *
 * The non-`id` fields are deliberately exactly {@link UpdateConversationRequestModel}, so this projects to
 * both request bodies without a third shape. Timestamps are deliberately absent — a draft has none, and
 * nothing observing the store reads them.
 */
export interface UaiConversationDetailModel {
    /** Undefined until persisted — the draft/persisted discriminator. */
    id?: string;
    projectId: string | null;
    title: string | null;
    agentIdOrAlias: string | null;
    profileId: string | null;
    contextIds: string[];
    resources: ContextResourceModel[];
    isPinned: boolean;
    isArchived: boolean;
}

/** An unsaved conversation; an optional project pre-attaches it. */
export function createConversationDraft(projectId?: string): UaiConversationDetailModel {
    return {
        projectId: projectId ?? null,
        title: null,
        agentIdOrAlias: null,
        profileId: null,
        contextIds: [],
        resources: [],
        isPinned: false,
        isArchived: false,
    };
}

/** Maps a loaded API conversation into the workspace detail model. Arrays are copied, not aliased. */
export function toConversationDetailModel(conversation: ConversationResponseModel): UaiConversationDetailModel {
    return {
        id: conversation.id,
        projectId: conversation.projectId ?? null,
        title: conversation.title ?? null,
        agentIdOrAlias: conversation.agentIdOrAlias ?? null,
        profileId: conversation.profileId ?? null,
        contextIds: [...conversation.contextIds],
        resources: [...conversation.resources],
        isPinned: conversation.isPinned,
        isArchived: conversation.isArchived,
    };
}

/**
 * Maps the detail model into the create body. Carries the draft's own contexts and resources so they are
 * persisted by the same request that creates the conversation — nothing has to survive the navigation to
 * the real conversation, which remounts the workspace and discards its store.
 */
export function toCreateConversationRequestModel(
    model: UaiConversationDetailModel,
): CreateConversationRequestModel {
    return {
        projectId: model.projectId,
        title: model.title?.trim() || null,
        agentIdOrAlias: model.agentIdOrAlias,
        profileId: model.profileId,
        contextIds: model.contextIds,
        resources: model.resources,
    };
}

/** Maps the detail model into the update body (the API takes the whole mutable state). */
export function toUpdateConversationRequestModel(
    model: UaiConversationDetailModel,
): UpdateConversationRequestModel {
    return {
        title: model.title?.trim() || null,
        projectId: model.projectId,
        agentIdOrAlias: model.agentIdOrAlias,
        profileId: model.profileId,
        contextIds: model.contextIds,
        resources: model.resources,
        isPinned: model.isPinned,
        isArchived: model.isArchived,
    };
}
