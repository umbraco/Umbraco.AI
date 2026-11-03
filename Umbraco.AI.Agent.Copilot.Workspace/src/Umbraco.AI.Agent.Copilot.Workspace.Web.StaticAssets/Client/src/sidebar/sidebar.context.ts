import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbArrayState, UmbObjectState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent } from "@umbraco-ai/core";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../project/repository/project.repository.js";
import {
    buildArchivedList,
    groupConversations,
    type UaiArchivedConversation,
    type UaiSidebarModel,
} from "../conversation/grouping.js";
import type { ConversationResponseModel, SharedConversationSummaryResponseModel } from "../conversation/types.js";
import { UAI_CONVERSATION_ENTITY_TYPE, UAI_PROJECT_ENTITY_TYPE } from "../constants.js";

const EMPTY_MODEL: UaiSidebarModel = { pinned: [], projects: [], recent: [], isEmpty: true };

/** Upper bound on the archived conversations fetched for the recycle-bin node (single, unpaged load). */
const ARCHIVED_TAKE = 200;

/** Upper bound on the shared-with-me conversations fetched for the sidebar group (single, unpaged load). */
const SHARED_WITH_ME_TAKE = 200;

/**
 * Section-scoped sidebar data context (provided by the shell). Owns conversation + project loading,
 * the search term, and the derived {@link UaiSidebarModel}, so the header search box and the three
 * group menus (Pinned / Projects / Recent — each its own sectionSidebarApp) stay in sync from one
 * source. Conversations refresh via the shared action-event bus; projects via their reactive store.
 */
export class UaiCopilotWorkspaceSidebarContext extends UmbContextBase {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);

    #conversations: ConversationResponseModel[] = [];
    /** Raw archived conversations (from a dedicated fetch), independent of the active search. */
    #archivedRaw: ConversationResponseModel[] = [];
    #projectNames = new Map<string, string>();

    #model = new UmbObjectState<UaiSidebarModel>(EMPTY_MODEL);
    #archived = new UmbArrayState<UaiArchivedConversation>([], (a) => a.conversation.id);
    #sharedWithMe = new UmbArrayState<SharedConversationSummaryResponseModel>([], (c) => c.conversationId);
    #search = new UmbStringState("");
    #activePath = new UmbStringState(window.location.pathname);

    readonly model = this.#model.asObservable();
    readonly pinned = this.#model.asObservablePart((m) => m.pinned);
    readonly projects = this.#model.asObservablePart((m) => m.projects);
    readonly recent = this.#model.asObservablePart((m) => m.recent);
    /** Archived conversations for the recycle-bin node — flat, most-recent-first, project name resolved. */
    readonly archived = this.#archived.asObservable();
    /** Conversations shared with the current user by someone else (SC-17), independent of ownership. */
    readonly sharedWithMe = this.#sharedWithMe.asObservable();
    readonly search = this.#search.asObservable();
    /** Current router path, for active highlighting across all group menus. */
    readonly activePath = this.#activePath.asObservable();

    #onNavigationEnd = () => this.#activePath.setValue(window.location.pathname);

    constructor(host: UmbControllerHost) {
        super(host, UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT);

        window.addEventListener("navigationend", this.#onNavigationEnd);

        // Both conversations and projects refresh off the shared action-event bus.
        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            context?.addEventListener(UaiEntityActionEvent.CREATED, this.#onEntityEvent as EventListener);
            context?.addEventListener(UaiEntityActionEvent.UPDATED, this.#onEntityEvent as EventListener);
            context?.addEventListener(UaiEntityActionEvent.DELETED, this.#onEntityEvent as EventListener);
        });

        void this.#loadProjects();
        void this.#load();
        void this.#loadArchived();
        void this.#loadSharedWithMe();
    }

    override destroy(): void {
        window.removeEventListener("navigationend", this.#onNavigationEnd);
        super.destroy();
    }

    getModel(): UaiSidebarModel {
        return this.#model.getValue();
    }

    setSearch(term: string): void {
        this.#search.setValue(term);
        void this.#load();
    }

    #onEntityEvent = (event: UaiEntityActionEvent) => {
        const type = event.getEntityType();
        if (type === UAI_CONVERSATION_ENTITY_TYPE) {
            // A conversation change may cross the active/archived boundary (archive, restore, delete),
            // so refresh both lists.
            void this.#load();
            void this.#loadArchived();
        } else if (type === UAI_PROJECT_ENTITY_TYPE) {
            void this.#loadProjects();
        }
    };

    async #load(): Promise<void> {
        const search = this.#search.getValue();
        const { data } = await this.#conversationRepository.requestCollection({ search: search || undefined });
        this.#conversations = data?.items ?? [];
        this.#recompute();
    }

    /**
     * Loads the archived conversations for the recycle-bin node. Deliberately independent of the active
     * search (the bin always shows all archived), fetched with `includeArchived` and filtered to the
     * archived subset in {@link buildArchivedList}.
     */
    async #loadArchived(): Promise<void> {
        const { data } = await this.#conversationRepository.requestCollection({
            includeArchived: true,
            take: ARCHIVED_TAKE,
        });
        // buildArchivedList filters to the archived subset itself, so store the raw page as-is.
        this.#archivedRaw = data?.items ?? [];
        this.#recomputeArchived();
    }

    /**
     * Loads the conversations shared with the current user (STORIES.md US3). A single, unpaged load,
     * independent of the owned-conversations search — no local event re-fires this: a share created or
     * revoked by the *owner* happens in a different user's session, so there is nothing to react to on
     * this side. AC3 ("revoked shares disappear when I open the sidebar") is satisfied by loading fresh
     * on each section mount, same as every other slice here.
     */
    async #loadSharedWithMe(): Promise<void> {
        const { data } = await this.#conversationRepository.requestSharedWithMe(0, SHARED_WITH_ME_TAKE);
        this.#sharedWithMe.setValue(data?.items ?? []);
    }

    async #loadProjects(): Promise<void> {
        const { data } = await this.#projectRepository.requestCollection();
        this.#projectNames = new Map((data?.items ?? []).map((p) => [p.id, p.name]));
        this.#recompute();
        // Archived chips resolve their project name from this map, so rebuild them too.
        this.#recomputeArchived();
    }

    #recompute(): void {
        // While searching, drop projects with no matching chat (conversations are already filtered).
        const searching = this.#search.getValue().trim().length > 0;
        this.#model.setValue(
            groupConversations(this.#conversations, this.#projectNames, { includeEmptyProjects: !searching }),
        );
    }

    #recomputeArchived(): void {
        this.#archived.setValue(buildArchivedList(this.#archivedRaw, this.#projectNames));
    }
}

export const UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT = new UmbContextToken<UaiCopilotWorkspaceSidebarContext>(
    "UaiCopilotWorkspaceSidebarContext",
);
