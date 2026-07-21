import { css, customElement, html, nothing, property, repeat, state, when } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import type { ProjectResponseModel } from "../../../api/types.gen.js";

/**
 * Right region: the context panel. Shows the context the open conversation runs with — its project's
 * instructions, attached context sets, and resources — mirroring what the backend injects server-side
 * from the conversation's project. Read-only here; the rich picker/editing lands with the projects UI
 * (Phase 6). Collapse/resize chrome is owned by the shell; this element renders the header (raising a
 * bubbling `collapse` event) and the body.
 *
 * Driven by `conversationId` (conversation route) or `projectId` (project route), set by the shell
 * from the active route. A request token guards against out-of-order resolution when the user switches
 * conversations quickly.
 */
@customElement("uai-copilot-workspace-context-panel")
export class UaiCopilotWorkspaceContextPanelElement extends UmbLitElement {
    #conversationRepository = new UaiConversationRepository(this);
    #projectRepository = new UaiProjectRepository(this);
    #requestToken = 0;

    @property({ type: String, attribute: false })
    conversationId?: string;

    @property({ type: String, attribute: false })
    projectId?: string;

    @state()
    private _loading = false;

    @state()
    private _project?: ProjectResponseModel;

    override willUpdate(changed: PropertyValues) {
        if (changed.has("conversationId") || changed.has("projectId")) {
            void this.#resolve();
        }
    }

    async #resolve() {
        const token = ++this.#requestToken;
        this._loading = true;
        this._project = undefined;

        const projectId = this.projectId ?? (await this.#projectIdForConversation(this.conversationId));
        if (token !== this.#requestToken) return; // superseded by a newer request

        if (!projectId) {
            this._loading = false;
            return;
        }

        const { data } = await this.#projectRepository.requestById(projectId);
        if (token !== this.#requestToken) return;

        this._project = data ?? undefined;
        this._loading = false;
    }

    async #projectIdForConversation(conversationId?: string): Promise<string | undefined> {
        if (!conversationId) return undefined;
        const { data } = await this.#conversationRepository.requestById(conversationId);
        return data?.projectId ?? undefined;
    }

    #collapse() {
        this.dispatchEvent(new CustomEvent("collapse", { bubbles: true, composed: true }));
    }

    override render() {
        return html`
            <div class="header">
                <span>${this.localize.term("uaiCopilotWorkspace_contextTitle")}</span>
                <uui-button
                    compact
                    look="secondary"
                    label=${this.localize.term("uaiCopilotWorkspace_contextCollapse")}
                    title=${this.localize.term("uaiCopilotWorkspace_contextCollapse")}
                    @click=${this.#collapse}
                >
                    <uui-icon name="icon-navigation-right"></uui-icon>
                </uui-button>
            </div>
            <div class="body">${this.#renderBody()}</div>
        `;
    }

    #renderBody() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }
        if (!this._project) {
            return html`<p class="muted">${this.localize.term("uaiCopilotWorkspace_contextNoProject")}</p>`;
        }
        return this.#renderProject(this._project);
    }

    #renderProject(project: ProjectResponseModel) {
        const resources = [...project.resources].sort((a, b) => a.sortOrder - b.sortOrder);
        const contextCount = project.contextIds.length;
        const hasAttachments = resources.length > 0 || contextCount > 0;

        return html`
            <h4 class="project-name">${project.name}</h4>

            ${when(
                project.instructions?.trim(),
                () => html`
                    <section>
                        <h5>${this.localize.term("uaiCopilotWorkspace_contextInstructionsHeading")}</h5>
                        <p class="instructions">${project.instructions}</p>
                    </section>
                `,
            )}

            <section>
                <h5>${this.localize.term("uaiCopilotWorkspace_contextAttachmentsHeading")}</h5>
                ${!hasAttachments
                    ? html`<p class="muted">${this.localize.term("uaiCopilotWorkspace_contextNoAttachments")}</p>`
                    : html`
                          ${contextCount > 0
                              ? html`<uui-tag look="secondary">
                                    ${this.localize.term("uaiCopilotWorkspace_contextContextCount", contextCount)}
                                </uui-tag>`
                              : nothing}
                          ${resources.length > 0
                              ? html`<uui-ref-list>
                                    ${repeat(
                                        resources,
                                        (r) => r.id,
                                        (r) => html`
                                            <uui-ref-node
                                                name=${r.name}
                                                detail=${r.description ?? r.resourceTypeId}
                                                readonly
                                            ></uui-ref-node>
                                        `,
                                    )}
                                </uui-ref-list>`
                              : nothing}
                      `}
            </section>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                background: var(--uui-color-surface);
            }
            .header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding: var(--uui-size-space-2) var(--uui-size-space-4);
                font-weight: bold;
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .body {
                flex: 1;
                overflow-y: auto;
                padding: var(--uui-size-space-4);
            }
            .project-name {
                margin: 0 0 var(--uui-size-space-4);
            }
            section {
                margin-bottom: var(--uui-size-space-5);
            }
            h5 {
                margin: 0 0 var(--uui-size-space-2);
                color: var(--uui-color-text-alt);
                text-transform: uppercase;
                font-size: 0.75rem;
                letter-spacing: 0.04em;
            }
            .instructions {
                margin: 0;
                white-space: pre-wrap;
            }
            .muted {
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceContextPanelElement;
