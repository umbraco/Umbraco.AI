import { css, customElement, html, nothing, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import type { UaiSidebarProject } from "../../conversation/grouping.js";
import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";
import { copilotWorkspaceProjectPath } from "../../paths.js";
import "./conversation-tree-item.element.js";

/**
 * A collapsible project node in the sidebar tree. Provides the project's `UMB_ENTITY_CONTEXT` so the
 * shared project entity actions (New chat / Delete) render in its ⋯ menu — the same definitions the
 * project workspace header uses. Clicking the name opens the project workspace; the caret toggles the
 * child conversations (expansion state is owned by the list, which persists it).
 */
@customElement("uai-copilot-workspace-project-tree-item")
export class UaiCopilotWorkspaceProjectTreeItemElement extends UmbLitElement {
    #entityContext = new UmbEntityContext(this);

    @property({ attribute: false })
    project?: UaiSidebarProject;

    @property({ type: Boolean })
    open = false;

    /** Current router path, for active highlighting (self + children). */
    @property({ type: String })
    activePath?: string;

    override willUpdate(changed: Map<PropertyKey, unknown>) {
        super.willUpdate(changed);
        if (changed.has("project") && this.project) {
            this.#entityContext.setEntityType(UAI_PROJECT_ENTITY_TYPE);
            this.#entityContext.setUnique(this.project.projectId);
        }
    }

    #toggle() {
        this.dispatchEvent(new CustomEvent("toggle", { bubbles: true, composed: true }));
    }

    override render() {
        const project = this.project;
        if (!project) return nothing;
        const path = copilotWorkspaceProjectPath(project.projectId);
        const active = this.activePath?.includes(path) ?? false;

        return html`
            <div class="node ${active ? "active" : ""}">
                <button
                    class="caret"
                    aria-label=${this.open ? "Collapse" : "Expand"}
                    aria-expanded=${this.open ? "true" : "false"}
                    @click=${this.#toggle}
                >
                    <uui-symbol-expand ?open=${this.open}></uui-symbol-expand>
                </button>
                <a class="label" href=${path}>
                    <uui-icon name="icon-folder"></uui-icon>
                    <span class="name">${project.name}</span>
                </a>
                <umb-entity-actions-dropdown class="actions" compact .label=${project.name}>
                    <uui-symbol-more slot="label"></uui-symbol-more>
                </umb-entity-actions-dropdown>
            </div>
            ${this.open ? this.#renderChildren(project) : nothing}
        `;
    }

    #renderChildren(project: UaiSidebarProject) {
        if (project.conversations.length === 0) {
            return html`<p class="empty">${this.localize.term("uaiCopilotWorkspace_projectNoConversations")}</p>`;
        }
        return html`
            <div class="children">
                ${repeat(
                    project.conversations,
                    (c) => c.id,
                    (c) => html`
                        <uai-copilot-workspace-conversation-tree-item
                            .conversation=${c}
                            .activePath=${this.activePath}
                        ></uai-copilot-workspace-conversation-tree-item>
                    `,
                )}
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }
            .node {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-1);
                padding-right: var(--uui-size-space-2);
                border-radius: var(--uui-border-radius);
            }
            .node:hover {
                background: var(--uui-color-surface-emphasis);
            }
            .node.active {
                background: var(--uui-color-current, var(--uui-color-surface-emphasis));
            }
            .caret {
                all: unset;
                display: inline-flex;
                align-items: center;
                cursor: pointer;
                padding: var(--uui-size-space-2);
            }
            .label {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                flex: 1;
                min-width: 0;
                padding: var(--uui-size-space-2) 0;
                color: inherit;
                text-decoration: none;
                font-weight: 700;
            }
            .name {
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
            .actions {
                flex: 0 0 auto;
            }
            .children {
                padding-left: var(--uui-size-space-5);
            }
            .empty {
                margin: 0;
                padding: 0 var(--uui-size-space-4) var(--uui-size-space-2) var(--uui-size-space-6);
                color: var(--uui-color-text-alt);
                font-size: 0.8em;
                font-style: italic;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceProjectTreeItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-project-tree-item": UaiCopilotWorkspaceProjectTreeItemElement;
    }
}
