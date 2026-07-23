import { css, customElement, html, query, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import type { UUIPopoverContainerElement } from "@umbraco-cms/backoffice/external/uui";
import { UaiConversationRepository } from "./repository/conversation.repository.js";
import { UaiProjectRepository } from "../project/repository/project.repository.js";
import { UAI_PROJECT_PICKER_MODAL } from "./modal/project-picker-modal.token.js";
import { copilotWorkspaceConversationPath, navigateToWorkspacePath } from "../paths.js";

/**
 * The primary "New chat" affordance — a CMS-style split button (à la Save and publish): the main
 * button starts a loose conversation, and the caret opens "New chat in a project", which pops a
 * central project picker and starts the conversation in the chosen project. Self-contained (owns its
 * repositories + navigation) so it can be dropped into both the sidebar header and the launcher.
 */
@customElement("uai-copilot-workspace-new-chat-button")
export class UaiCopilotWorkspaceNewChatButtonElement extends UmbLitElement {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);

    @state() private _open = false;

    @query("#new-chat-menu") private _popover?: UUIPopoverContainerElement;

    #navigate(id: string) {
        navigateToWorkspacePath(copilotWorkspaceConversationPath(id));
    }

    async #newChat() {
        const { data } = await this.#conversationRepository.create({});
        if (data?.id) this.#navigate(data.id);
    }

    async #newChatInProject() {
        this._popover?.hidePopover();
        const { data } = await this.#projectRepository.requestCollection();
        const projects = data?.items ?? [];
        // No projects yet — fall back to a loose conversation rather than a dead end.
        if (projects.length === 0) return this.#newChat();

        const chosen = await umbOpenModal(this, UAI_PROJECT_PICKER_MODAL, {
            data: {
                projects: projects.map((p) => ({ id: p.id, name: p.name, description: p.description })),
            },
        }).catch(() => undefined);
        if (!chosen) return;

        const { data: conversation } = await this.#conversationRepository.create({ projectId: chosen.projectId });
        if (conversation?.id) this.#navigate(conversation.id);
    }

    override render() {
        const newChat = this.localize.term("uaiCopilotWorkspace_newChat");
        return html`
            <uui-button-group>
                <uui-button look="primary" label=${newChat} @click=${this.#newChat}>
                    <uui-icon name="icon-add"></uui-icon>
                    ${newChat}
                </uui-button>
                <uui-button
                    look="primary"
                    compact
                    popovertarget="new-chat-menu"
                    label=${this.localize.term("uaiCopilotWorkspace_newChatInProject")}
                >
                    <uui-symbol-expand .open=${this._open}></uui-symbol-expand>
                </uui-button>
            </uui-button-group>
            <uui-popover-container
                id="new-chat-menu"
                placement="bottom-end"
                @beforetoggle=${(e: ToggleEvent) => (this._open = e.newState === "open")}
            >
                <umb-popover-layout>
                    <uui-menu-item
                        label=${this.localize.term("uaiCopilotWorkspace_newChatInProject")}
                        @click=${this.#newChatInProject}
                    >
                        <uui-icon slot="icon" name="icon-folder"></uui-icon>
                    </uui-menu-item>
                </umb-popover-layout>
            </uui-popover-container>
        `;
    }

    static override styles = [
        css`
            :host,
            uui-button-group {
                width: 100%;
            }
            /* uui-button-group grows all children equally; keep the caret sized to its content. */
            uui-button-group > uui-button:last-child {
                flex: 0 0 auto;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceNewChatButtonElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-new-chat-button": UaiCopilotWorkspaceNewChatButtonElement;
    }
}
