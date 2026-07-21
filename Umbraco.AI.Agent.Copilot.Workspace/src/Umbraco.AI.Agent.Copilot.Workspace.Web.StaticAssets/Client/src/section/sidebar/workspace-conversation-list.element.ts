import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbSectionSidebarAppElement } from "@umbraco-cms/backoffice/section";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { debounce } from "@umbraco-cms/backoffice/utils";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { UaiConversationRepository } from "../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import { groupConversations, type UaiConversationGroup } from "../../conversation/grouping.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import {
    copilotWorkspaceConversationPath,
    copilotWorkspaceProjectPath,
    UAI_COPILOT_WORKSPACE_DASHBOARD_PATH,
} from "../../paths.js";

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

    override connectedCallback() {
        super.connectedCallback();
        window.addEventListener("navigationend", this.#onNavigationEnd);
        this.#load();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        window.removeEventListener("navigationend", this.#onNavigationEnd);
    }

    async #load() {
        this._loading = true;

        const [conversationsResult, projectsResult] = await Promise.all([
            this.#conversationRepository.requestCollection({ search: this._search || undefined }),
            this.#projectRepository.requestCollection(),
        ]);

        const conversations = conversationsResult.data?.items ?? [];
        const projectNames = new Map<string, string>(
            (projectsResult.data?.items ?? []).map((p) => [p.id, p.name]),
        );

        this._groups = groupConversations(conversations, projectNames, Date.now());
        this._empty = conversations.length === 0;
        this._loading = false;
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
        const { data } = await this.#projectRepository.create({
            name: this.localize.term("uaiCopilotWorkspace_newProjectDefaultName"),
            contextIds: [],
            resources: [],
        });
        if (data?.id) {
            this.#navigateTo(copilotWorkspaceProjectPath(data.id));
            await this.#load();
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
            this.#navigateTo(UAI_COPILOT_WORKSPACE_DASHBOARD_PATH);
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
                ${repeat(
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
