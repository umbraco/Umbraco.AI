import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbBooleanState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";

/**
 * The thing the workspace is pointed at, set synchronously from the route — a persisted conversation
 * (`id`) or a `draft` (nothing persisted yet). The chat runtime keys its thread on this the moment the
 * route changes, before the conversation metadata has loaded.
 */
export interface UaiConversationTarget {
    id?: string;
    isDraft: boolean;
}
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConversationRepository } from "../repository/conversation.repository.js";
import { UaiConversationUpdatedController } from "../conversation-updated.controller.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import {
    createConversationDraft,
    toConversationDetailModel,
    toCreateConversationRequestModel,
    toUpdateConversationRequestModel,
} from "../types.js";
import type { ContextResourceModel, UaiConversationDetailModel } from "../types.js";
import type { ProjectResponseModel } from "../../project/types.js";

/**
 * Store context for the open conversation — the single source of truth the conversation workspace provides
 * and both regions (center chat, right context panel) observe. It owns the conversation being edited and
 * its inherited project, is the sole action-bus subscriber for the open conversation, and the sole writer
 * (edits update {@link conversation$} optimistically — the PUT returns 204, and this avoids a refetch).
 * This is the Umbraco workspace/store-context pattern: consumers react, they don't fetch.
 *
 * A **draft** (nothing persisted yet) is held in the very same model, discriminated only by a missing `id`.
 * That is what lets consumers — notably the context panel — treat a draft exactly like a saved
 * conversation: they read and write the model and never ask whether it exists server-side. Draft edits are
 * buffered in the model and persisted by the single create request {@link commitDraft} sends on the first
 * message, because navigating to the real conversation remounts the workspace and discards this store.
 */
export class UaiConversationWorkspaceContext extends UmbContextBase {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);
    #requestToken = 0;

    /** In-flight own writes; the action-bus reload skips while >0 so we don't refetch our own edits. */
    #selfWrites = 0;
    /** True while {@link commitDraft}'s create is in flight, so a second commit can't double-create. */
    #committing = false;

    #model = new UmbObjectState<UaiConversationDetailModel | undefined>(undefined);
    #project = new UmbObjectState<ProjectResponseModel | undefined>(undefined);
    #resolved = new UmbBooleanState(false);
    #target = new UmbObjectState<UaiConversationTarget>({ isDraft: false });

    /**
     * The conversation being edited — persisted, or an unsaved draft (no `id`). Undefined only while a
     * persisted conversation is still loading, or when it failed to load.
     */
    readonly conversation$ = this.#model.asObservable();
    /** The conversation's owning project, for the inherited context layer (undefined if none). */
    readonly project$ = this.#project.asObservable();
    /** True while the conversation is archived — consumers render read-only. */
    readonly isReadonly$ = this.#model.asObservablePart((c) => c?.isArchived ?? false);
    /** True once the current target's mode is known (loaded, or a draft) — gates the composer flash. */
    readonly isResolved$ = this.#resolved.asObservable();
    /** The route-set target (conversation id or draft), emitted synchronously for the chat runtime. */
    readonly target$ = this.#target.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONVERSATION_WORKSPACE_CONTEXT);

        // Reflect an external change to the open conversation (e.g. archive/move from its menu ⋯ actions)
        // so every consumer updates in place. One subscription for the whole workspace. Own writes are
        // skipped (they already updated the state optimistically and dispatched the event for the menu).
        new UaiConversationUpdatedController(this, () => this.#model.getValue()?.id, () => {
            if (this.#selfWrites === 0) void this.#reload();
        });
    }

    isDraft(): boolean {
        return this.#target.getValue().isDraft;
    }

    /** Opens a persisted conversation: loads it and its owning project, newest state wins. */
    async setConversationId(conversationId: string): Promise<void> {
        if (!this.isDraft() && this.#model.getValue()?.id === conversationId) return;
        this.#resolved.setValue(false);
        this.#target.setValue({ id: conversationId, isDraft: false });
        await this.#reload(conversationId);
    }

    /** Enters draft mode (nothing persisted yet); an optional project pre-attaches the eventual chat. */
    async startDraft(projectId?: string): Promise<void> {
        const token = ++this.#requestToken;
        // A draft is editable and its mode is known immediately — set the model first so the panel never
        // sees "resolved but nothing to edit".
        this.#model.setValue(createConversationDraft(projectId));
        this.#resolved.setValue(true);
        this.#target.setValue({ isDraft: true });
        await this.#loadProject(projectId, token);

        // A stale or unauthorized ?projectId= in the URL would otherwise be posted as a dangling reference.
        if (token === this.#requestToken && projectId && !this.#project.getValue()) {
            const model = this.#model.getValue();
            if (model && !model.id) this.#model.setValue({ ...model, projectId: null });
        }
    }

    async #reload(conversationId?: string): Promise<void> {
        const token = ++this.#requestToken;
        const id = conversationId ?? this.#model.getValue()?.id;
        if (!id) return;

        const { data } = await this.#conversationRepository.requestById(id);
        if (token !== this.#requestToken) return; // superseded by a newer open
        this.#model.setValue(data ? toConversationDetailModel(data) : undefined);
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
        this.#write({ contextIds });
    }

    setResources(resources: ContextResourceModel[]): void {
        this.#write({ resources });
    }

    setAgentIdOrAlias(agentIdOrAlias: string): void {
        this.#write({ agentIdOrAlias });
    }

    rename(title: string): void {
        this.#write({ title });
    }

    /**
     * Applies a field change to the conversation being edited. Persisting is the only thing that differs
     * between a draft and a saved conversation: a draft buffers, and {@link commitDraft} sends the lot.
     * For a saved conversation the change is applied optimistically and then PUT — no refetch.
     */
    #write(patch: Partial<Omit<UaiConversationDetailModel, "id">>): void {
        const current = this.#model.getValue();
        if (!current) return;
        const next = { ...current, ...patch };
        this.#model.setValue(next);
        if (!next.id) return;

        // Persisting dispatches an UPDATED event (so the menu refreshes); count it as our own so our own
        // action-bus subscriber doesn't treat it as an external change and refetch what we just wrote.
        this.#selfWrites++;
        void this.#conversationRepository
            .update(next.id, toUpdateConversationRequestModel(next))
            .finally(() => {
                this.#selfWrites = Math.max(0, this.#selfWrites - 1);
            });
    }

    /**
     * Persists the draft — the one request that writes everything buffered on it (project, agent, and its
     * own contexts and resources). Returns the new id, or undefined on failure, in which case the draft is
     * left untouched so the caller can retry without the user losing what they attached.
     *
     * Deliberately does NOT touch {@link target$}: emitting a new target here would fire into the chat
     * context that is about to be torn down, reset its run controller and race the caller's navigation.
     *
     * NOTE: the body is a snapshot taken when the create starts. An edit made while it is in flight is
     * lost (accepted: the window is one request, and the alternatives are a compensating PUT or freezing
     * the panel).
     */
    async commitDraft(title?: string): Promise<string | undefined> {
        const model = this.#model.getValue();
        if (!model || model.id || this.#committing) return undefined;

        this.#committing = true;
        try {
            const { data } = await this.#conversationRepository.create(
                toCreateConversationRequestModel({ ...model, title: model.title ?? title ?? null }),
            );
            if (!data?.id) return undefined;
            this.#model.setValue(toConversationDetailModel(data));
            return data.id;
        } finally {
            this.#committing = false;
        }
    }
}

export const UAI_CONVERSATION_WORKSPACE_CONTEXT = new UmbContextToken<UaiConversationWorkspaceContext>(
    "UaiConversationWorkspaceContext",
);
