import type {
    ContextResourceModel,
    ConversationResponseModel,
    ConversationShareResponseModel,
    CreateConversationRequestModel,
    PagedSharedConversationSummaryResponseModel,
    ShareConversationRequestModel,
    SharedConversationResponseModel,
    SharedConversationSummaryResponseModel,
    UpdateConversationRequestModel,
} from "../api/types.gen.js";

export type {
    ContextResourceModel,
    ConversationResponseModel,
    ConversationShareResponseModel,
    CreateConversationRequestModel,
    PagedSharedConversationSummaryResponseModel,
    ShareConversationRequestModel,
    SharedConversationResponseModel,
    SharedConversationSummaryResponseModel,
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
 * The mutable, server-writable fields (`projectId` through `isArchived`) are deliberately exactly
 * {@link UpdateConversationRequestModel}, so this projects to both request bodies without a third shape.
 * `isShared`/`readonly`/`ownerDisplayName` are store-only — derived at mapping time, never sent back to the
 * server. Timestamps are deliberately absent — a draft has none, and nothing observing the store reads them.
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
    /** True for a conversation opened via a recipient's shared view rather than owned by the current user. */
    isShared: boolean;
    /**
     * Derived once at mapping time (`isArchived || isShared`) — the one field every consumer (the chat view,
     * the context panel) reads to decide whether writes are ever valid, rather than each site individually
     * growing an `|| isShared` check of its own.
     */
    readonly: boolean;
    /**
     * The sharing owner's display name, present only for a shared conversation (undefined for an owned one).
     * Lives on the detail model — rather than, say, a separate field on the store — so both the chat view's
     * read-only notice and the sidebar's "shared with me" badge tooltip (SC-13/17) can read it off the one
     * model the workspace store already exposes, instead of each needing its own fetch/plumbing.
     */
    ownerDisplayName?: string;
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
        isShared: false,
        readonly: false,
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
        isShared: false,
        readonly: conversation.isArchived,
    };
}

/**
 * Maps a recipient's shared view of a conversation into the workspace detail model. `isPinned`/`isArchived`
 * are left at their defaults — those are owner-only concepts a share never carries — and `messages` is
 * deliberately not consumed here: a shared conversation's chat history loads through a different mechanism
 * (`UaiServerPersistedConversationStrategy`), wired up by a later task (SC-18).
 */
export function toSharedConversationDetailModel(
    conversation: SharedConversationResponseModel,
): UaiConversationDetailModel {
    return {
        id: conversation.id,
        projectId: null,
        title: conversation.title ?? null,
        agentIdOrAlias: conversation.agentIdOrAlias ?? null,
        profileId: conversation.profileId ?? null,
        contextIds: [...conversation.contextIds],
        resources: [...conversation.resources],
        isPinned: false,
        isArchived: false,
        isShared: true,
        readonly: true,
        ownerDisplayName: conversation.ownerDisplayName,
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
