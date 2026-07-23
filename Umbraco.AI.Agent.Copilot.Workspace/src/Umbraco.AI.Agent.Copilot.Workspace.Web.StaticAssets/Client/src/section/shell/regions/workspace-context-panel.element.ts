import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiContextPickerElement } from "@umbraco-ai/core";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import type { ConversationResponseModel } from "../../../conversation/types.js";
import type { ContextResourceModel, ProjectResponseModel } from "../../../project/types.js";

/** Minimal structural type for the globally-registered (but not type-exported) `uai-resource-list`. */
interface ResourceListElement extends HTMLElement {
    items: ContextResourceModel[];
}

/**
 * Right region: the context panel. Shows the context the open conversation runs with, in two stacked
 * layers that mirror what the backend injects server-side:
 *  - <b>inherited from the project</b> (read-only) — its instructions, attached contexts, resources;
 *  - <b>this conversation</b> (editable) — contexts and resources attached to this conversation only,
 *    which stack on top of the project's rather than replacing them.
 * Each concept is its own auto-open collapsible block. Collapse/resize chrome and the open/close
 * toggle are owned by the shell; this element only renders the header title and the body.
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

    @state()
    private _conversation?: ConversationResponseModel;

    override willUpdate(changed: PropertyValues) {
        if (changed.has("conversationId") || changed.has("projectId")) {
            void this.#resolve();
        }
    }

    async #resolve() {
        const token = ++this.#requestToken;
        this._loading = true;
        this._project = undefined;
        this._conversation = undefined;

        // Conversation route: load the conversation first (carries its own contexts/resources), then
        // resolve its owning project for the inherited layer.
        if (this.conversationId) {
            const { data } = await this.#conversationRepository.requestById(this.conversationId);
            if (token !== this.#requestToken) return; // superseded by a newer request
            this._conversation = data ?? undefined;
        }

        const projectId = this.projectId ?? this._conversation?.projectId ?? undefined;
        if (projectId) {
            const { data } = await this.#projectRepository.requestById(projectId);
            if (token !== this.#requestToken) return;
            this._project = data ?? undefined;
        }

        this._loading = false;
    }

    #onContextsChange(event: Event) {
        if (!this._conversation) return;
        const value = ((event.target as UaiContextPickerElement).value as string[] | undefined) ?? [];
        this._conversation = { ...this._conversation, contextIds: value };
        void this.#conversationRepository.setContextIds(this._conversation, value);
    }

    #onResourcesChange(event: Event) {
        if (!this._conversation) return;
        const items = [...(event.target as ResourceListElement).items];
        this._conversation = { ...this._conversation, resources: items };
        void this.#conversationRepository.setResources(this._conversation, items);
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
        if (!this._conversation && !this._project) {
            return html`<p class="muted">${this.localize.term("uaiCopilotWorkspace_contextNoProject")}</p>`;
        }

        const project = this._project;
        const conversation = this._conversation;
        const instructions = project?.instructions?.trim();
        const projectContextIds = project?.contextIds ?? [];
        const projectResources = project ? [...project.resources].sort((a, b) => a.sortOrder - b.sortOrder) : [];

        return html`
            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextInstructionsHeading",
                instructions
                    ? html`<p class="instructions">${instructions}</p>`
                    : this.#renderEmpty("uaiCopilotWorkspace_contextNoInstructions"),
            )}
            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextContextsHeading",
                this.#renderContexts(projectContextIds, conversation),
            )}
            ${this.#renderBlock(
                "uaiCopilotWorkspace_contextResourcesHeading",
                this.#renderResources(projectResources, conversation),
            )}
        `;
    }

    // Contexts and resources render as a single list per block: the project's items are shown locked
    // (read-only, no remove affordance) above the conversation's own editable items and a slim add
    // control. No "from project"/"this conversation" labels — the missing remove button is the cue.
    #renderContexts(projectContextIds: Array<string>, conversation?: ConversationResponseModel) {
        if (projectContextIds.length === 0 && !conversation) {
            return this.#renderEmpty("uaiCopilotWorkspace_contextNoContexts");
        }
        return html`
            ${projectContextIds.length > 0
                ? html`<uai-context-picker
                      readonly
                      multiple
                      .value=${projectContextIds}
                      .readonlyHint=${this.localize.term("uaiCopilotWorkspace_contextInheritedHint")}
                  ></uai-context-picker>`
                : nothing}
            ${conversation
                ? html`<uai-context-picker
                      multiple
                      .value=${conversation.contextIds}
                      @change=${this.#onContextsChange}
                  ></uai-context-picker>`
                : nothing}
        `;
    }

    #renderResources(projectResources: Array<ContextResourceModel>, conversation?: ConversationResponseModel) {
        if (projectResources.length === 0 && !conversation) {
            return this.#renderEmpty("uaiCopilotWorkspace_contextNoResources");
        }
        return html`
            ${projectResources.length > 0
                ? html`<uai-resource-list
                      compact
                      readonly
                      .items=${projectResources}
                      .readonlyHint=${this.localize.term("uaiCopilotWorkspace_contextInheritedHint")}
                  ></uai-resource-list>`
                : nothing}
            ${conversation
                ? html`<uai-resource-list
                      compact
                      .items=${conversation.resources}
                      @change=${this.#onResourcesChange}
                  ></uai-resource-list>`
                : nothing}
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
            .block-body > uai-context-picker,
            .block-body > uai-resource-list {
                display: block;
            }
            /* Resources use the compact list (divider-separated rows with no divider between two
               stacked lists), so add one at the boundary in the same colour as the row dividers.
               Contexts use the normal picker view — boundary styling handled there. */
            .block-body > uai-resource-list + uai-resource-list,
            .block-body > uai-context-picker + uai-context-picker {
                border-top: 1px solid var(--uui-color-border);
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
