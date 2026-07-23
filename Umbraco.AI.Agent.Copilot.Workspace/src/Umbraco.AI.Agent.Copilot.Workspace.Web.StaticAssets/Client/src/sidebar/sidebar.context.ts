import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UmbObjectState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent } from "@umbraco-ai/core";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../project/repository/project.repository.js";
import { groupConversations, type UaiSidebarModel } from "../conversation/grouping.js";
import type { ConversationResponseModel } from "../conversation/types.js";
import { UAI_CONVERSATION_ENTITY_TYPE, UAI_PROJECT_ENTITY_TYPE } from "../constants.js";

const EMPTY_MODEL: UaiSidebarModel = { pinned: [], projects: [], recent: [], isEmpty: true };

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
    #projectNames = new Map<string, string>();

    #model = new UmbObjectState<UaiSidebarModel>(EMPTY_MODEL);
    #search = new UmbStringState("");
    #activePath = new UmbStringState(window.location.pathname);

    readonly model = this.#model.asObservable();
    readonly pinned = this.#model.asObservablePart((m) => m.pinned);
    readonly projects = this.#model.asObservablePart((m) => m.projects);
    readonly recent = this.#model.asObservablePart((m) => m.recent);
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
        if (type === UAI_CONVERSATION_ENTITY_TYPE) void this.#load();
        else if (type === UAI_PROJECT_ENTITY_TYPE) void this.#loadProjects();
    };

    async #load(): Promise<void> {
        const search = this.#search.getValue();
        const { data } = await this.#conversationRepository.requestCollection({ search: search || undefined });
        this.#conversations = data?.items ?? [];
        this.#recompute();
    }

    async #loadProjects(): Promise<void> {
        const { data } = await this.#projectRepository.requestCollection();
        this.#projectNames = new Map((data?.items ?? []).map((p) => [p.id, p.name]));
        this.#recompute();
    }

    #recompute(): void {
        // While searching, drop projects with no matching chat (conversations are already filtered).
        const searching = this.#search.getValue().trim().length > 0;
        this.#model.setValue(
            groupConversations(this.#conversations, this.#projectNames, { includeEmptyProjects: !searching }),
        );
    }
}

export const UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT = new UmbContextToken<UaiCopilotWorkspaceSidebarContext>(
    "UaiCopilotWorkspaceSidebarContext",
);
