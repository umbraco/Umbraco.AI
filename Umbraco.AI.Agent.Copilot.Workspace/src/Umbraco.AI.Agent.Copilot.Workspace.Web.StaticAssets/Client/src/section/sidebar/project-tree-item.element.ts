import { css, customElement, html, nothing, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import type { UaiSidebarProject } from "../../conversation/grouping.js";
import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";
import { copilotWorkspaceProjectPath } from "../../paths.js";
import "./conversation-tree-item.element.js";

/**
 * A collapsible project node in the sidebar tree. Uses `uui-menu-item` (the same chrome as
 * conversation nodes) so the ⋯ actions reveal on hover and align consistently, and the caret /
 * children come from the component itself. Provides the project's `UMB_ENTITY_CONTEXT` so the shared
 * project entity actions (New chat / Delete) render in its ⋯ menu. Clicking the name opens the project
 * workspace; the caret toggles the child conversations (expansion state is owned + persisted by the
 * list).
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

    // The caret fires show-children/hide-children; we own the (persisted) state, so preventDefault and
    // let the list flip it via the `toggle` event.
    #onToggle = (event: Event) => {
        event.preventDefault();
        event.stopPropagation();
        this.dispatchEvent(new CustomEvent("toggle", { bubbles: true, composed: true }));
    };

    override render() {
        const project = this.project;
        if (!project) return nothing;
        const path = copilotWorkspaceProjectPath(project.projectId);
        const active = this.activePath?.includes(path) ?? false;

        return html`
            <uui-menu-item
                label=${project.name}
                href=${path}
                ?active=${active}
                .hasChildren=${true}
                .showChildren=${this.open}
                @show-children=${this.#onToggle}
                @hide-children=${this.#onToggle}
            >
                <uui-icon slot="icon" name="icon-folder"></uui-icon>
                <umb-entity-actions-dropdown slot="actions" compact .label=${project.name}>
                    <uui-symbol-more slot="label"></uui-symbol-more>
                </umb-entity-actions-dropdown>
                ${this.open ? this.#renderChildren(project) : nothing}
            </uui-menu-item>
        `;
    }

    #renderChildren(project: UaiSidebarProject) {
        if (project.conversations.length === 0) {
            return html`<p class="empty">${this.localize.term("uaiCopilotWorkspace_projectNoConversations")}</p>`;
        }
        return repeat(
            project.conversations,
            (c) => c.id,
            (c) => html`
                <uai-copilot-workspace-conversation-tree-item
                    .conversation=${c}
                    .activePath=${this.activePath}
                ></uai-copilot-workspace-conversation-tree-item>
            `,
        );
    }

    static override styles = [
        css`
            :host {
                display: block;
            }
            .empty {
                margin: 0;
                padding: var(--uui-size-space-1) var(--uui-size-space-4) var(--uui-size-space-2);
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
