import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import type { ConversationResponseModel } from "../../../conversation/types.js";
import { copilotWorkspaceConversationPath } from "../../../paths.js";
import "../../../conversation/new-chat-button.element.js";

const RECENT_LIMIT = 6;

/**
 * The section landing/launcher (main workspace area when nothing is open). Leads the user into
 * starting a conversation with the shared New chat split button (New chat / New chat in a project),
 * plus a short list of recent conversations to resume. The sidebar tree remains the persistent way to
 * browse everything else.
 */
@customElement("uai-copilot-workspace-launcher")
export class UaiCopilotWorkspaceLauncherElement extends UmbLitElement {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);

    @state() private _recent: ConversationResponseModel[] = [];
    @state() private _projectNames = new Map<string, string>();

    override connectedCallback() {
        super.connectedCallback();
        void this.#loadRecent();
        void this.#loadProjects();
    }

    async #loadRecent() {
        const { data } = await this.#conversationRepository.requestCollection({ take: RECENT_LIMIT });
        this._recent = data?.items ?? [];
    }

    async #loadProjects() {
        const { data } = await this.#projectRepository.requestCollection();
        this._projectNames = new Map((data?.items ?? []).map((p) => [p.id, p.name]));
    }

    #open(path: string) {
        window.history.pushState({}, "", path);
    }

    override render() {
        return html`
            <div class="launcher">
                <uui-icon name="icon-chat"></uui-icon>
                <h2>${this.localize.term("uaiCopilotWorkspace_launcherHeading")}</h2>
                <p class="subtitle">${this.localize.term("uaiCopilotWorkspace_launcherSubtitle")}</p>

                <div class="actions">
                    <uai-copilot-workspace-new-chat-button></uai-copilot-workspace-new-chat-button>
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
                                              detail=${(c.projectId && this._projectNames.get(c.projectId)) || ""}
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
                justify-content: center;
            }
            .actions uai-copilot-workspace-new-chat-button {
                width: auto;
                min-width: 240px;
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
