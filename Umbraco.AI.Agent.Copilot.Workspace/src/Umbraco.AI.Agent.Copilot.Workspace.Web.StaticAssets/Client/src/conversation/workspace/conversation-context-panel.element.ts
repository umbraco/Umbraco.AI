import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiContextPickerElement } from "@umbraco-ai/core";
import { UAI_CONVERSATION_WORKSPACE_CONTEXT, type UaiConversationWorkspaceContext } from "./conversation-workspace.context.js";
import type { ConversationResponseModel } from "../types.js";
import type { ContextResourceModel, ProjectResponseModel } from "../../project/types.js";

/** Minimal structural type for the globally-registered (but not type-exported) `uai-resource-list`. */
interface ResourceListElement extends HTMLElement {
    items: ContextResourceModel[];
}

/**
 * Right region of the conversation workspace: the context the open conversation runs with, in two
 * stacked layers that mirror what the backend injects server-side:
 *  - <b>inherited from the project</b> (read-only) — its instructions, attached contexts, resources;
 *  - <b>this conversation</b> (editable) — contexts and resources attached to this conversation only,
 *    which stack on top of the project's rather than replacing them.
 *
 * Purely reactive: it observes the workspace store (conversation + owning project) and routes its edits
 * back through the store's writers — it never fetches or tracks the conversation itself. When the
 * conversation is archived the whole panel is read-only.
 */
@customElement("uai-copilot-workspace-conversation-context-panel")
export class UaiCopilotWorkspaceConversationContextPanelElement extends UmbLitElement {
    #store?: UaiConversationWorkspaceContext;

    @state() private _resolved = false;
    @state() private _project?: ProjectResponseModel;
    @state() private _conversation?: ConversationResponseModel;

    constructor() {
        super();
        this.consumeContext(UAI_CONVERSATION_WORKSPACE_CONTEXT, (store) => {
            this.#store = store;
            this.observe(store?.conversation$, (c) => (this._conversation = c));
            this.observe(store?.project$, (p) => (this._project = p));
            this.observe(store?.isResolved$, (r) => (this._resolved = r ?? false));
        });
    }

    #onContextsChange(event: Event) {
        const value = ((event.target as UaiContextPickerElement).value as string[] | undefined) ?? [];
        this.#store?.setContexts(value);
    }

    #onResourcesChange(event: Event) {
        this.#store?.setResources([...(event.target as ResourceListElement).items]);
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
        if (!this._resolved) {
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
            ${project
                ? this.#renderBlock(
                      "uaiCopilotWorkspace_contextInstructionsHeading",
                      instructions
                          ? html`<p class="instructions">${instructions}</p>`
                          : this.#renderEmpty("uaiCopilotWorkspace_contextNoInstructions"),
                  )
                : nothing}
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
    // (read-only) above the conversation's own editable items and a slim add control. The divider is only
    // drawn when the editable picker actually has items (has-items class). When archived, all read-only.
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
                      .readonlyHint=${conversation?.isArchived
                          ? undefined
                          : this.localize.term("uaiCopilotWorkspace_contextInheritedHint")}
                  ></uai-context-picker>`
                : nothing}
            ${conversation
                ? html`<uai-context-picker
                      class=${conversation.contextIds.length > 0 ? "has-items" : ""}
                      multiple
                      ?readonly=${conversation.isArchived}
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
                      .readonlyHint=${conversation?.isArchived
                          ? undefined
                          : this.localize.term("uaiCopilotWorkspace_contextInheritedHint")}
                  ></uai-resource-list>`
                : nothing}
            ${conversation
                ? html`<uai-resource-list
                      class=${conversation.resources.length > 0 ? "has-items" : ""}
                      compact
                      ?readonly=${conversation.isArchived}
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
            /* Match the modal / workspace header: full header height, surface background, h3 headline,
               bottom border in the standard border color. */
            .header {
                display: flex;
                align-items: center;
                box-sizing: border-box;
                height: var(--umb-header-layout-height);
                /* Left padding matches the body content; right padding clears the layout's collapse toggle. */
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
            /* Divider between the inherited (project) layer and the conversation's own layer. Only drawn
               when the second (editable) picker actually has items — an empty one shows just its add
               control, where a boundary line would be noise. The has-items class is toggled reactively. */
            .block-body > uai-resource-list + uai-resource-list.has-items,
            .block-body > uai-context-picker + uai-context-picker.has-items {
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

export default UaiCopilotWorkspaceConversationContextPanelElement;
