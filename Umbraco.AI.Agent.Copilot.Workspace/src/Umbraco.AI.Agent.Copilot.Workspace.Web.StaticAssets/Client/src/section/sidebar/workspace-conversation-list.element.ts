import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbSectionSidebarAppElement } from "@umbraco-cms/backoffice/section";
import { debounce } from "@umbraco-cms/backoffice/utils";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { UaiConversationRepository } from "../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import { groupConversations, type UaiSidebarModel } from "../../conversation/grouping.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import {
    copilotWorkspaceConversationPath,
    copilotWorkspaceProjectCreatePath,
} from "../../paths.js";
import { UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT } from "../../constants.js";
import "./conversation-tree-item.element.js";
import "./project-tree-item.element.js";

const STORAGE_EXPANDED = "uai-cw-expanded-projects";
const EMPTY_MODEL: UaiSidebarModel = { pinned: [], projects: [], recent: [], isEmpty: true };

/**
 * The conversation list, rendered inside the section shell's sidebar. A CMS-style tree: a header with
 * a create (+) menu and search, then Pinned, a collapsible Projects tree (one node per project,
 * empty ones included), and a flat Recent list of project-less conversations. Data comes from the
 * conversation collection + the reactive project store; per-node ⋯ menus use the standard
 * entity-action system (see the conversation/project entity actions). Selecting an item navigates the
 * main-area router; the shell renders the chat/workspace.
 */
@customElement("uai-copilot-workspace-conversation-list")
export class UaiCopilotWorkspaceConversationListElement
    extends UmbLitElement
    implements UmbSectionSidebarAppElement
{
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);

    /** Last-loaded conversations (re-fetched on change/search); combined with the reactive projects. */
    #conversations: ConversationResponseModel[] = [];

    /** Projects (id → name) from the reactive project store; drives the tree. */
    #projectNames = new Map<string, string>();

    /** Project ids the user has explicitly expanded (persisted). */
    #expanded = readExpanded();

    @state()
    private _loading = true;

    @state()
    private _search = "";

    @state()
    private _model: UaiSidebarModel = EMPTY_MODEL;

    /** Absolute path of the currently open route (for active highlighting + auto-expand). */
    @state()
    private _activePath = window.location.pathname;

    #onNavigationEnd = () => {
        this._activePath = window.location.pathname;
    };

    /** Reload when a conversation changes elsewhere (auto-title, entity actions, chat view). */
    #onConversationsChanged = () => {
        this.#load();
    };

    override connectedCallback() {
        super.connectedCallback();
        window.addEventListener("navigationend", this.#onNavigationEnd);
        window.addEventListener(UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT, this.#onConversationsChanged);

        // Projects are reactive: observe the store so the tree re-groups (incl. new empty project
        // nodes) whenever a project is created/renamed/deleted anywhere — no manual reload.
        this.observe(this.#projectRepository.projectItems$, (projects) => {
            this.#projectNames = new Map([...projects].map(([id, p]) => [id, p.name]));
            this.#recompute();
        });
        void this.#projectRepository.initialize();

        this.#load();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        window.removeEventListener("navigationend", this.#onNavigationEnd);
        window.removeEventListener(UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT, this.#onConversationsChanged);
    }

    /** Re-fetches conversations (projects come from the reactive store) and re-groups. */
    async #load() {
        this._loading = true;
        const { data } = await this.#conversationRepository.requestCollection({ search: this._search || undefined });
        this.#conversations = data?.items ?? [];
        this._loading = false;
        this.#recompute();
    }

    #recompute() {
        this._model = groupConversations(this.#conversations, this.#projectNames);
    }

    #debouncedSearch = debounce(() => this.#load(), 250);

    #onSearchInput(event: InputEvent) {
        this._search = (event.target as UUIInputElement).value?.toString() ?? "";
        this.#debouncedSearch();
    }

    #navigateTo(path: string) {
        window.history.pushState({}, "", path);
    }

    async #onNewChat() {
        const { data } = await this.#conversationRepository.create({});
        if (data?.id) {
            this.#navigateTo(copilotWorkspaceConversationPath(data.id));
            await this.#load();
        }
    }

    #onNewProject() {
        // Open the project workspace in "create" mode; it scaffolds an unsaved project and creates
        // it on Save (which dispatches CREATED → the reactive tree adds the node).
        this.#navigateTo(copilotWorkspaceProjectCreatePath());
    }

    /** Id of the conversation open in the main area, if any (for auto-expanding its project). */
    #activeConversationId(): string | undefined {
        return this._activePath.match(/\/conversation\/([^/]+)/)?.[1];
    }

    #isProjectOpen(projectId: string, hasActiveChild: boolean): boolean {
        return this.#expanded.has(projectId) || hasActiveChild;
    }

    #toggleProject(projectId: string) {
        if (this.#expanded.has(projectId)) this.#expanded.delete(projectId);
        else this.#expanded.add(projectId);
        writeExpanded(this.#expanded);
        this.requestUpdate();
    }

    override render() {
        return html`
            <div class="header">
                <div class="title-row">
                    <span class="title">${this.localize.term("uaiCopilotWorkspace_sectionLabel")}</span>
                    <umb-dropdown
                        compact
                        hide-expand
                        placement="bottom-end"
                        label=${this.localize.term("uaiCopilotWorkspace_treeCreate")}
                    >
                        <span slot="label" class="create-trigger" title=${this.localize.term("uaiCopilotWorkspace_treeCreate")}>
                            <uui-icon name="icon-add"></uui-icon>
                        </span>
                        <uui-menu-item
                            label=${this.localize.term("uaiCopilotWorkspace_newChat")}
                            @click=${this.#onNewChat}
                        >
                            <uui-icon slot="icon" name="icon-add"></uui-icon>
                        </uui-menu-item>
                        <uui-menu-item
                            label=${this.localize.term("uaiCopilotWorkspace_newProject")}
                            @click=${this.#onNewProject}
                        >
                            <uui-icon slot="icon" name="icon-folder"></uui-icon>
                        </uui-menu-item>
                    </umb-dropdown>
                </div>
                <uui-input
                    type="search"
                    .value=${this._search}
                    placeholder=${this.localize.term("uaiCopilotWorkspace_searchPlaceholder")}
                    label=${this.localize.term("uaiCopilotWorkspace_searchPlaceholder")}
                    @input=${this.#onSearchInput}
                ></uui-input>
            </div>
            <div class="list">${this.#renderList()}</div>
        `;
    }

    #renderList() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }
        if (this._model.isEmpty) {
            const key = this._search ? "uaiCopilotWorkspace_listNoResults" : "uaiCopilotWorkspace_listEmpty";
            return html`<p class="muted">${this.localize.term(key)}</p>`;
        }
        return html`
            ${this.#renderFlatSection("uaiCopilotWorkspace_groupPinned", this._model.pinned)}
            ${this.#renderProjects()}
            ${this.#renderFlatSection("uaiCopilotWorkspace_treeRecentHeading", this._model.recent)}
        `;
    }

    #renderFlatSection(labelKey: string, conversations: ConversationResponseModel[]) {
        if (conversations.length === 0) return nothing;
        return html`
            <div class="section">
                <div class="section-header">${this.localize.term(labelKey)}</div>
                ${repeat(
                    conversations,
                    (c) => c.id,
                    (c) => html`
                        <uai-copilot-workspace-conversation-tree-item
                            .conversation=${c}
                            .activePath=${this._activePath}
                        ></uai-copilot-workspace-conversation-tree-item>
                    `,
                )}
            </div>
        `;
    }

    #renderProjects() {
        if (this._model.projects.length === 0) return nothing;
        const activeId = this.#activeConversationId();
        return html`
            <div class="section">
                <div class="section-header">${this.localize.term("uaiCopilotWorkspace_treeProjectsHeading")}</div>
                ${repeat(
                    this._model.projects,
                    (p) => p.projectId,
                    (p) => {
                        const hasActiveChild = !!activeId && p.conversations.some((c) => c.id === activeId);
                        return html`
                            <uai-copilot-workspace-project-tree-item
                                .project=${p}
                                .activePath=${this._activePath}
                                ?open=${this.#isProjectOpen(p.projectId, hasActiveChild)}
                                @toggle=${() => this.#toggleProject(p.projectId)}
                            ></uai-copilot-workspace-project-tree-item>
                        `;
                    },
                )}
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                min-height: 0;
            }
            .header {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4);
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .title-row {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }
            .title {
                font-weight: 700;
            }
            .create-trigger {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                cursor: pointer;
                color: var(--uui-color-interactive);
            }
            .create-trigger:hover {
                color: var(--uui-color-interactive-emphasis);
            }
            .header uui-input {
                width: 100%;
            }
            .list {
                flex: 1;
                overflow-y: auto;
                padding: var(--uui-size-space-2) 0;
            }
            .section {
                margin-bottom: var(--uui-size-space-3);
            }
            .section-header {
                padding: var(--uui-size-space-2) var(--uui-size-space-4);
                font-size: 0.75rem;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0.04em;
                color: var(--uui-color-text-alt);
            }
            .muted {
                padding: 0 var(--uui-size-space-4);
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

function readExpanded(): Set<string> {
    try {
        const raw = localStorage.getItem(STORAGE_EXPANDED);
        return new Set(raw ? (JSON.parse(raw) as string[]) : []);
    } catch {
        return new Set();
    }
}

function writeExpanded(set: Set<string>): void {
    try {
        localStorage.setItem(STORAGE_EXPANDED, JSON.stringify([...set]));
    } catch {
        /* storage unavailable — in-session only */
    }
}

export default UaiCopilotWorkspaceConversationListElement;
