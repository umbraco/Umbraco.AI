import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbBooleanState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";

/**
 * The thing the workspace is pointed at, set synchronously from the route — a persisted conversation
 * (`id`) or a `draft` (nothing persisted yet, optionally pre-attached to `projectId`). The chat runtime
 * keys its thread on this the moment the route changes, before the conversation metadata has loaded.
 */
export interface UaiConversationTarget {
    id?: string;
    isDraft: boolean;
    projectId?: string;
}
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { ContextResourceModel } from "../../api/types.gen.js";
import { UaiConversationRepository } from "../repository/conversation.repository.js";
import { UaiConversationUpdatedController } from "../conversation-updated.controller.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import type { ConversationResponseModel } from "../types.js";
import type { ProjectResponseModel } from "../../project/types.js";

/**
 * Store context for an open conversation — the single source of truth the conversation workspace
 * provides and both regions (center chat, right context panel) observe. It owns the loaded conversation
 * and its inherited project, is the sole action-bus subscriber for the open conversation, and the sole
 * writer (edits update {@link conversation$} optimistically — the PUT returns 204, and this avoids a
 * refetch). This is the Umbraco workspace/store-context pattern: consumers react, they don't fetch.
 *
 * Draft mode ({@link startDraft}) represents "no conversation persisted yet"; the conversation is created
 * on the first sent message by the chat context, which then navigates to the real id.
 */
export class UaiConversationWorkspaceContext extends UmbContextBase {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);
    #requestToken = 0;

    #conversationId?: string;
    #isDraft = false;
    #draftProjectId?: string;
    /** In-flight own writes; the action-bus reload skips while >0 so we don't refetch our own edits. */
    #selfWrites = 0;

    #conversation = new UmbObjectState<ConversationResponseModel | undefined>(undefined);
    #project = new UmbObjectState<ProjectResponseModel | undefined>(undefined);
    #resolved = new UmbBooleanState(false);
    #target = new UmbObjectState<UaiConversationTarget>({ isDraft: false });

    /** The open conversation (undefined while a draft, still loading, or missing). */
    readonly conversation$ = this.#conversation.asObservable();
    /** The conversation's owning project, for the inherited context layer (undefined if none). */
    readonly project$ = this.#project.asObservable();
    /** True while the conversation is archived — consumers render read-only. */
    readonly isReadonly$ = this.#conversation.asObservablePart((c) => c?.isArchived ?? false);
    /** The open conversation id (undefined for a draft), for the chat runtime to key its thread on. */
    readonly conversationId$ = this.#conversation.asObservablePart((c) => c?.id);
    /** True once the current target's mode is known (loaded, or a draft) — gates the composer flash. */
    readonly isResolved$ = this.#resolved.asObservable();
    /** The route-set target (conversation id or draft), emitted synchronously for the chat runtime. */
    readonly target$ = this.#target.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONVERSATION_WORKSPACE_CONTEXT);

        // Reflect an external change to the open conversation (e.g. archive/move from its menu ⋯ actions)
        // so every consumer updates in place. One subscription for the whole workspace. Own writes are
        // skipped (they already updated the state optimistically and dispatched the event for the menu).
        new UaiConversationUpdatedController(this, () => this.#conversationId, () => {
            if (this.#selfWrites === 0) void this.#reload();
        });
    }

    getConversation(): ConversationResponseModel | undefined {
        return this.#conversation.getValue();
    }

    getConversationId(): string | undefined {
        return this.#conversationId;
    }

    isDraft(): boolean {
        return this.#isDraft;
    }

    /** The project a draft is pre-attached to (applied when the draft is created), if any. */
    getDraftProjectId(): string | undefined {
        return this.#draftProjectId;
    }

    /** Opens a persisted conversation: loads it and its owning project, newest state wins. */
    async setConversationId(conversationId: string): Promise<void> {
        if (!this.#isDraft && this.#conversationId === conversationId) return;
        this.#conversationId = conversationId;
        this.#isDraft = false;
        this.#draftProjectId = undefined;
        this.#resolved.setValue(false);
        this.#target.setValue({ id: conversationId, isDraft: false });
        await this.#reload();
    }

    /** Enters draft mode (nothing persisted yet); an optional project pre-attaches the eventual chat. */
    async startDraft(projectId?: string): Promise<void> {
        this.#conversationId = undefined;
        this.#isDraft = true;
        this.#draftProjectId = projectId;
        this.#conversation.setValue(undefined);
        // A draft is editable and its mode is known immediately (no conversation to fetch).
        this.#resolved.setValue(true);
        this.#target.setValue({ isDraft: true, projectId });
        await this.#loadProject(projectId);
    }

    async #reload(): Promise<void> {
        const token = ++this.#requestToken;
        const id = this.#conversationId;
        if (!id) return;

        const { data } = await this.#conversationRepository.requestById(id);
        if (token !== this.#requestToken) return; // superseded by a newer open
        this.#conversation.setValue(data ?? undefined);
        this.#resolved.setValue(true);
        await this.#loadProject(data?.projectId ?? undefined, token);
    }

    async #loadProject(projectId: string | null | undefined, token = this.#requestToken): Promise<void> {
        if (!projectId) {
            this.#project.setValue(undefined);
            return;
        }
        const { data } = await this.#projectRepository.requestById(projectId);
        if (token !== this.#requestToken) return;
        this.#project.setValue(data ?? undefined);
    }

    // --- Writers (optimistic; the update endpoint returns 204) ---

    setContexts(contextIds: string[]): void {
        this.#write((c) => ({ ...c, contextIds }), (c) => this.#conversationRepository.setContextIds(c, contextIds));
    }

    setResources(resources: ContextResourceModel[]): void {
        this.#write((c) => ({ ...c, resources }), (c) => this.#conversationRepository.setResources(c, resources));
    }

    setAgentIdOrAlias(agentIdOrAlias: string): void {
        this.#write(
            (c) => ({ ...c, agentIdOrAlias }),
            (c) => this.#conversationRepository.setAgentIdOrAlias(c, agentIdOrAlias),
        );
    }

    rename(title: string): void {
        this.#write((c) => ({ ...c, title }), (c) => this.#conversationRepository.rename(c, title));
    }

    /** Optimistically applies a field change to the loaded conversation, then persists it. No refetch. */
    #write(
        apply: (c: ConversationResponseModel) => ConversationResponseModel,
        persist: (c: ConversationResponseModel) => Promise<unknown>,
    ): void {
        const current = this.#conversation.getValue();
        if (!current) return;
        this.#conversation.setValue(apply(current));
        // Persisting dispatches an UPDATED event (so the menu refreshes); count it as our own so our own
        // action-bus subscriber doesn't treat it as an external change and refetch what we just wrote.
        this.#selfWrites++;
        void persist(current).finally(() => {
            this.#selfWrites = Math.max(0, this.#selfWrites - 1);
        });
    }
}

export const UAI_CONVERSATION_WORKSPACE_CONTEXT = new UmbContextToken<UaiConversationWorkspaceContext>(
    "UaiConversationWorkspaceContext",
);
