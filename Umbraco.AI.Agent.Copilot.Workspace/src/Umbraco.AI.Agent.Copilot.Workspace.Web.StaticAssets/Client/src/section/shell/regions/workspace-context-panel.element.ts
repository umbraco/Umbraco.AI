import { css, customElement, html, repeat, property, state } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import type { ProjectResponseModel } from "../../../api/types.gen.js";

/**
 * Right region: the context panel. Shows the context the open conversation runs with — its project's
 * instructions, attached context sets, and resources — mirroring what the backend injects server-side
 * from the conversation's project. Read-only here; the rich picker/editing lands with the projects UI
 * (Phase 6). Each concept is its own auto-open collapsible block. Collapse/resize chrome and the
 * open/close toggle are owned by the shell; this element only renders the header title and the body.
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

    override render() {
        return html`
            <div class="header">
                <h3>${this.localize.term("uaiCopilotWorkspace_contextTitle")}</h3>
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
        const instructions = project.instructions?.trim();

        return html`
            <h4 class="project-name">${project.name}</h4>

            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextInstructionsHeading",
                instructions
                    ? html`<p class="instructions">${instructions}</p>`
                    : this.#renderEmpty("uaiCopilotWorkspace_contextNoInstructions"),
            )}
            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextContextsHeading",
                project.contextIds.length > 0
                    ? html`<uai-context-picker readonly multiple .value=${project.contextIds}></uai-context-picker>`
                    : this.#renderEmpty("uaiCopilotWorkspace_contextNoContexts"),
            )}
            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextResourcesHeading",
                resources.length > 0
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
                    : this.#renderEmpty("uaiCopilotWorkspace_contextNoResources"),
            )}
        `;
    }

    /** An auto-open collapsible block with a localized heading and rotating chevron. */
    #renderBlock(headingKey: string, content: unknown) {
        return html`
            <details class="block" open>
                <summary>
                    <uui-icon class="chevron" name="icon-navigation-right"></uui-icon>
                    <span>${this.localize.term(headingKey)}</span>
                </summary>
                <div class="block-body">${content}</div>
            </details>
        `;
    }

    #renderEmpty(key: string) {
        return html`<p class="muted">${this.localize.term(key)}</p>`;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                background: var(--uui-color-surface);
            }
            /* Match the modal / workspace header (umb-body-layout): full header height,
               surface background, h3 headline, bottom border in the standard border color. */
            .header {
                display: flex;
                align-items: center;
                box-sizing: border-box;
                height: var(--umb-header-layout-height);
                /* Left padding matches the body content so the title left-aligns with the blocks;
                   right padding clears the shell's absolutely-positioned collapse toggle. */
                padding: 0 2.5rem 0 var(--uui-size-space-4);
                background: var(--uui-color-surface);
                border-bottom: 1px solid var(--uui-color-border);
            }
            .header h3 {
                margin: 0;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
            /* No horizontal padding here — the header's and blocks' bottom borders run edge to edge;
               inner content carries its own horizontal padding instead. */
            .body {
                flex: 1;
                overflow-y: auto;
                padding: 0;
            }
            .body > .muted,
            .body > uui-loader {
                display: block;
                padding: var(--uui-size-space-4);
            }
            .project-name {
                margin: 0;
                padding: var(--uui-size-space-4) var(--uui-size-space-4) var(--uui-size-space-3);
            }
            .block {
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .block > summary {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                cursor: pointer;
                list-style: none;
                user-select: none;
                font-weight: bold;
                color: var(--uui-color-text-alt);
                text-transform: uppercase;
                font-size: 0.75rem;
                letter-spacing: 0.04em;
            }
            /* Hide the native disclosure triangle (we render our own chevron). */
            .block > summary::-webkit-details-marker {
                display: none;
            }
            .block > summary:hover {
                color: var(--uui-color-text);
            }
            .chevron {
                transition: transform 120ms ease;
                font-size: 0.8em;
            }
            .block[open] > summary > .chevron {
                transform: rotate(90deg);
            }
            .block-body {
                padding: 0 var(--uui-size-space-4) var(--uui-size-space-4);
            }
            .instructions {
                margin: 0;
                white-space: pre-wrap;
            }
            .muted {
                margin: 0;
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceContextPanelElement;
