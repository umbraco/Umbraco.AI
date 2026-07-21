import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbSectionSidebarAppElement } from "@umbraco-cms/backoffice/section";
import { umbConfirmModal, umbOpenModal, UMB_ITEM_PICKER_MODAL } from "@umbraco-cms/backoffice/modal";
import { debounce } from "@umbraco-cms/backoffice/utils";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { UaiConversationRepository } from "../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import { groupConversations, type UaiConversationGroup } from "../../conversation/grouping.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import {
    copilotWorkspaceConversationPath,
    copilotWorkspaceProjectPath,
    UAI_COPILOT_WORKSPACE_SECTION_PATH,
} from "../../paths.js";
import { UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT } from "../../constants.js";

/**
 * The conversation list, rendered as a section-sidebar app so it uses the CMS's standard section
 * sidebar chrome (placement, width, global collapse). Data-bound over the generated management API
 * client: lists conversations (+ projects for grouping), and offers New Chat, search, and per-item
 * pin / rename / archive / delete. Selecting an item navigates the main-area router
 * (`/…/workspace/conversation/:id`); the shell renders the chat.
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

    /** Projects (id → name) from the reactive project store; drives grouping + the move picker. */
    #projectNames = new Map<string, string>();

    @state()
    private _loading = true;

    @state()
    private _search = "";

    @state()
    private _groups: UaiConversationGroup[] = [];

    @state()
    private _empty = false;

    @state()
    private _renamingId?: string;

    /** Absolute path of the currently open conversation (for active highlighting). */
    @state()
    private _activePath = window.location.pathname;

    #onNavigationEnd = () => {
        this._activePath = window.location.pathname;
    };

    /** Reload when a conversation changes elsewhere in the section (e.g. auto-titled by the chat). */
    #onConversationsChanged = () => {
        this.#load();
    };

    override connectedCallback() {
        super.connectedCallback();
        window.addEventListener("navigationend", this.#onNavigationEnd);
        window.addEventListener(UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT, this.#onConversationsChanged);

        // Projects are reactive: observe the store so the tree re-groups (incl. new empty project
        // folders) whenever a project is created/renamed/deleted anywhere — no manual reload.
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
        this._groups = groupConversations(this.#conversations, this.#projectNames, Date.now());
        this._empty = this._groups.length === 0;
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

    async #onNewProject() {
        // The reactive project store updates the tree (empty folder appears) on the dispatched
        // CREATED event — no manual reload needed here.
        const { data } = await this.#projectRepository.create({
            name: this.localize.term("uaiCopilotWorkspace_newProjectDefaultName"),
            contextIds: [],
            resources: [],
        });
        if (data?.id) {
            this.#navigateTo(copilotWorkspaceProjectPath(data.id));
        }
    }

    async #onTogglePin(conversation: ConversationResponseModel) {
        await this.#conversationRepository.setPinned(conversation, !conversation.isPinned);
        await this.#load();
    }

    async #onArchive(conversation: ConversationResponseModel) {
        await this.#conversationRepository.setArchived(conversation, !conversation.isArchived);
        await this.#load();
    }

    async #onMoveToProject(conversation: ConversationResponseModel) {
        // Mirror the CMS "Move to" UX with the generic item picker (our items aren't tree entities):
        // a flat list of projects plus a "No project" option to detach. Projects come from the
        // reactive store, so the list is always current.
        const noProjectValue = "";
        const items = [
            { label: this.localize.term("uaiCopilotWorkspace_moveNoProject"), value: noProjectValue, icon: "icon-delete" },
            ...[...this.#projectNames].map(([id, name]) => ({ label: name, value: id, icon: "icon-folder" })),
        ];

        let chosen;
        try {
            chosen = await umbOpenModal(this, UMB_ITEM_PICKER_MODAL, {
                data: { headline: this.localize.term("uaiCopilotWorkspace_moveHeadline"), items },
            });
        } catch {
            return; // cancelled
        }

        const projectId = chosen.value === noProjectValue ? null : chosen.value;
        if ((conversation.projectId ?? null) === projectId) return; // no change
        await this.#conversationRepository.moveToProject(conversation, projectId);
        await this.#load();
    }

    async #onDelete(conversation: ConversationResponseModel) {
        await umbConfirmModal(this, {
            headline: this.localize.term("uaiCopilotWorkspace_deleteConfirmTitle"),
            content: this.localize.term("uaiCopilotWorkspace_deleteConfirmMessage"),
            color: "danger",
            confirmLabel: this.localize.term("uaiCopilotWorkspace_actionDelete"),
        });

        const { error } = await this.#conversationRepository.delete(conversation.id);
        if (error) return;

        // If the deleted conversation is open, fall back to the section landing.
        if (this._activePath.includes(copilotWorkspaceConversationPath(conversation.id))) {
            this.#navigateTo(UAI_COPILOT_WORKSPACE_SECTION_PATH);
        }
        await this.#load();
    }

    #startRename(conversation: ConversationResponseModel) {
        this._renamingId = conversation.id;
    }

    async #commitRename(conversation: ConversationResponseModel, value: string) {
        this._renamingId = undefined;
        const title = value.trim();
        if (!title || title === (conversation.title ?? "")) return;
        await this.#conversationRepository.rename(conversation, title);
        await this.#load();
    }

    override render() {
        return html`
            <div class="header">
                <div class="new-actions">
                    <uui-button
                        look="primary"
                        label=${this.localize.term("uaiCopilotWorkspace_newChat")}
                        @click=${this.#onNewChat}
                    >
                        <uui-icon name="icon-add"></uui-icon>
                        ${this.localize.term("uaiCopilotWorkspace_newChat")}
                    </uui-button>
                    <uui-button
                        look="secondary"
                        label=${this.localize.term("uaiCopilotWorkspace_newProject")}
                        title=${this.localize.term("uaiCopilotWorkspace_newProject")}
                        @click=${this.#onNewProject}
                    >
                        <uui-icon name="icon-folder"></uui-icon>
                    </uui-button>
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
        if (this._empty) {
            const key = this._search ? "uaiCopilotWorkspace_listNoResults" : "uaiCopilotWorkspace_listEmpty";
            return html`<p class="muted">${this.localize.term(key)}</p>`;
        }
        return repeat(
            this._groups,
            (group) => group.key,
            (group) => this.#renderGroup(group),
        );
    }

    #renderGroup(group: UaiConversationGroup) {
        return html`
            <div class="group">
                <div class="group-header">
                    ${group.kind === "project" && group.projectId
                        ? html`<a class="group-link" href=${copilotWorkspaceProjectPath(group.projectId)}>${group.label}</a>`
                        : html`<span>${group.label.startsWith("#") ? this.localize.term(group.label.slice(1)) : group.label}</span>`}
                </div>
                ${group.conversations.length === 0
                    ? html`<p class="empty-project">${this.localize.term("uaiCopilotWorkspace_projectNoConversations")}</p>`
                    : repeat(
                          group.conversations,
                          (c) => c.id,
                          (c) => this.#renderItem(c),
                      )}
            </div>
        `;
    }

    #renderItem(conversation: ConversationResponseModel) {
        const href = copilotWorkspaceConversationPath(conversation.id);
        const active = this._activePath.includes(href);
        const title = conversation.title?.trim() || this.localize.term("uaiCopilotWorkspace_untitledConversation");

        if (this._renamingId === conversation.id) {
            return html`
                <uui-input
                    class="rename-input"
                    .value=${conversation.title ?? ""}
                    label=${this.localize.term("uaiCopilotWorkspace_renamePrompt")}
                    autofocus
                    @keydown=${(e: KeyboardEvent) => {
                        if (e.key === "Enter") this.#commitRename(conversation, (e.target as UUIInputElement).value?.toString() ?? "");
                        if (e.key === "Escape") this._renamingId = undefined;
                    }}
                    @blur=${(e: FocusEvent) => this.#commitRename(conversation, (e.target as UUIInputElement).value?.toString() ?? "")}
                ></uui-input>
            `;
        }

        return html`
            <uui-menu-item label=${title} href=${href} ?active=${active}>
                ${conversation.isPinned ? html`<uui-icon slot="icon" name="icon-pin"></uui-icon>` : nothing}
                <div slot="actions" @click=${(e: Event) => e.stopPropagation()}>
                    ${this.#renderActions(conversation)}
                </div>
            </uui-menu-item>
        `;
    }

    #renderActions(conversation: ConversationResponseModel) {
        return html`
            <umb-dropdown compact hide-expand placement="bottom-end" label="Actions">
                <uui-symbol-more slot="label"></uui-symbol-more>
                <uui-menu-item
                    label=${this.localize.term(conversation.isPinned ? "uaiCopilotWorkspace_actionUnpin" : "uaiCopilotWorkspace_actionPin")}
                    @click=${() => this.#onTogglePin(conversation)}
                >
                    <uui-icon slot="icon" name="icon-pin"></uui-icon>
                </uui-menu-item>
                <uui-menu-item
                    label=${this.localize.term("uaiCopilotWorkspace_actionRename")}
                    @click=${() => this.#startRename(conversation)}
                >
                    <uui-icon slot="icon" name="icon-edit"></uui-icon>
                </uui-menu-item>
                <uui-menu-item
                    label=${this.localize.term("uaiCopilotWorkspace_actionMoveToProject")}
                    @click=${() => this.#onMoveToProject(conversation)}
                >
                    <uui-icon slot="icon" name="icon-enter"></uui-icon>
                </uui-menu-item>
                <uui-menu-item
                    label=${this.localize.term(conversation.isArchived ? "uaiCopilotWorkspace_actionUnarchive" : "uaiCopilotWorkspace_actionArchive")}
                    @click=${() => this.#onArchive(conversation)}
                >
                    <uui-icon slot="icon" name="icon-box"></uui-icon>
                </uui-menu-item>
                <uui-menu-item
                    label=${this.localize.term("uaiCopilotWorkspace_actionDelete")}
                    @click=${() => this.#onDelete(conversation)}
                >
                    <uui-icon slot="icon" name="icon-trash"></uui-icon>
                </uui-menu-item>
            </umb-dropdown>
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
            .header uui-input {
                width: 100%;
            }
            .new-actions {
                display: flex;
                gap: var(--uui-size-space-2);
            }
            .new-actions uui-button:first-child {
                flex: 1;
            }
            .list {
                flex: 1;
                overflow-y: auto;
                padding: var(--uui-size-space-2) 0;
            }
            .group {
                margin-bottom: var(--uui-size-space-3);
            }
            .group-header {
                padding: var(--uui-size-space-2) var(--uui-size-space-4);
                font-size: 0.75rem;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0.04em;
                color: var(--uui-color-text-alt);
            }
            .group-link {
                color: inherit;
                text-decoration: none;
            }
            .group-link:hover {
                text-decoration: underline;
            }
            .empty-project {
                margin: 0;
                padding: 0 var(--uui-size-space-4) var(--uui-size-space-2);
                color: var(--uui-color-text-alt);
                font-size: 0.8em;
                font-style: italic;
            }
            .rename-input {
                display: block;
                margin: 0 var(--uui-size-space-3);
                width: calc(100% - 2 * var(--uui-size-space-3));
            }
            .muted {
                padding: 0 var(--uui-size-space-4);
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationListElement;
