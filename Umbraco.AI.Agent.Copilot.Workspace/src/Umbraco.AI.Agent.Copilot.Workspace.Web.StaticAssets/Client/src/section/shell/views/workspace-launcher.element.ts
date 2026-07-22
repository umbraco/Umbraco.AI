import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbOpenModal, UMB_ITEM_PICKER_MODAL } from "@umbraco-cms/backoffice/modal";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import type { ConversationResponseModel } from "../../../conversation/types.js";
import { copilotWorkspaceConversationPath } from "../../../paths.js";

const RECENT_LIMIT = 6;

/**
 * The section landing/launcher (main workspace area when nothing is open). Leads the user into
 * starting a conversation — a prominent "New chat", an optional "start in a project" picker, and a
 * short list of recent conversations to resume. Selecting any of these opens the conversation
 * workspace; the sidebar tree remains the persistent way to browse everything else.
 */
@customElement("uai-copilot-workspace-launcher")
export class UaiCopilotWorkspaceLauncherElement extends UmbLitElement {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);

    @state() private _recent: ConversationResponseModel[] = [];

    override connectedCallback() {
        super.connectedCallback();
        void this.#loadRecent();
    }

    async #loadRecent() {
        const { data } = await this.#conversationRepository.requestCollection({ take: RECENT_LIMIT });
        this._recent = data?.items ?? [];
    }

    #open(path: string) {
        window.history.pushState({}, "", path);
    }

    async #startConversation(projectId?: string) {
        const { data } = await this.#conversationRepository.create(projectId ? { projectId } : {});
        if (data?.id) this.#open(copilotWorkspaceConversationPath(data.id));
    }

    async #startInProject() {
        const { data } = await this.#projectRepository.requestCollection();
        const projects = data?.items ?? [];
        if (projects.length === 0) {
            // No projects yet — just start a loose conversation.
            void this.#startConversation();
            return;
        }
        try {
            const chosen = await umbOpenModal(this, UMB_ITEM_PICKER_MODAL, {
                data: {
                    headline: this.localize.term("uaiCopilotWorkspace_launcherStartInProject"),
                    items: projects.map((p) => ({ label: p.name, value: p.id, icon: "icon-folder" })),
                },
            });
            void this.#startConversation(chosen.value);
        } catch {
            /* cancelled */
        }
    }

    override render() {
        return html`
            <div class="launcher">
                <uui-icon name="icon-chat"></uui-icon>
                <h2>${this.localize.term("uaiCopilotWorkspace_launcherHeading")}</h2>
                <p class="subtitle">${this.localize.term("uaiCopilotWorkspace_launcherSubtitle")}</p>

                <div class="actions">
                    <uui-button
                        look="primary"
                        label=${this.localize.term("uaiCopilotWorkspace_newChat")}
                        @click=${() => this.#startConversation()}
                    >
                        <uui-icon name="icon-add"></uui-icon>
                        ${this.localize.term("uaiCopilotWorkspace_newChat")}
                    </uui-button>
                    <uui-button
                        look="secondary"
                        label=${this.localize.term("uaiCopilotWorkspace_launcherStartInProject")}
                        @click=${this.#startInProject}
                    >
                        <uui-icon name="icon-folder"></uui-icon>
                        ${this.localize.term("uaiCopilotWorkspace_launcherStartInProject")}
                    </uui-button>
                </div>

                ${this._recent.length
                    ? html`
                          <div class="recent">
                              <h3>${this.localize.term("uaiCopilotWorkspace_launcherRecent")}</h3>
                              <uui-ref-list>
                                  ${repeat(
                                      this._recent,
                                      (c) => c.id,
                                      (c) => html`
                                          <uui-ref-node
                                              name=${c.title?.trim() || this.localize.term("uaiCopilotWorkspace_untitledConversation")}
                                              @open=${() => this.#open(copilotWorkspaceConversationPath(c.id))}
                                              @click=${() => this.#open(copilotWorkspaceConversationPath(c.id))}
                                          >
                                              <uui-icon slot="icon" name="icon-chat"></uui-icon>
                                          </uui-ref-node>
                                      `,
                                  )}
                              </uui-ref-list>
                          </div>
                      `
                    : nothing}
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: grid;
                place-items: center;
                height: 100%;
                overflow-y: auto;
            }
            .launcher {
                width: 100%;
                max-width: 560px;
                text-align: center;
                padding: var(--uui-size-layout-1);
            }
            .launcher > uui-icon {
                font-size: 3rem;
                color: var(--uui-color-text-alt);
                opacity: 0.6;
            }
            h2 {
                margin: var(--uui-size-space-4) 0 var(--uui-size-space-2);
            }
            .subtitle {
                margin: 0 0 var(--uui-size-space-5);
                color: var(--uui-color-text-alt);
            }
            .actions {
                display: flex;
                gap: var(--uui-size-space-3);
                justify-content: center;
                flex-wrap: wrap;
            }
            .recent {
                margin-top: var(--uui-size-layout-1);
                text-align: left;
            }
            .recent h3 {
                font-size: 0.8rem;
                text-transform: uppercase;
                letter-spacing: 0.04em;
                color: var(--uui-color-text-alt);
                margin: 0 0 var(--uui-size-space-2);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceLauncherElement;
