import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";

// Region elements (side-effect imports register the custom elements used in the template).
import "./regions/workspace-conversation-list.element.js";
import "./regions/workspace-context-panel.element.js";

/**
 * The Copilot Workspace three-region shell: a conversation list (left), a routed main area (center),
 * and a context panel (right). The center hosts an <umb-router-slot> with the section's routes:
 * empty landing, an open conversation (/conversation/:id), and a project (/project/:id).
 */
@customElement("uai-copilot-workspace-shell")
export class UaiCopilotWorkspaceShellElement extends UmbLitElement {
    @state()
    private _routes: UmbRoute[] = [
        {
            path: "conversation/:id",
            component: () => import("./views/workspace-conversation-view.element.js"),
            setup: (component, info) => {
                (component as { conversationId?: string }).conversationId = info.match.params.id;
            },
        },
        {
            path: "project/:id",
            component: () => import("./views/workspace-project-view.element.js"),
            setup: (component, info) => {
                (component as { projectId?: string }).projectId = info.match.params.id;
            },
        },
        {
            path: "",
            component: () => import("./views/workspace-empty-view.element.js"),
        },
        {
            path: "**",
            component: async () => (await import("@umbraco-cms/backoffice/router")).UmbRouteNotFoundElement,
        },
    ];

    override render() {
        return html`
            <div class="shell">
                <uai-copilot-workspace-conversation-list></uai-copilot-workspace-conversation-list>
                <main>
                    <umb-router-slot .routes=${this._routes}></umb-router-slot>
                </main>
                <uai-copilot-workspace-context-panel></uai-copilot-workspace-context-panel>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
            }
            .shell {
                display: grid;
                grid-template-columns: 320px minmax(0, 1fr) 340px;
                height: 100%;
                min-height: 0;
            }
            main {
                min-width: 0;
                height: 100%;
                overflow: hidden;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceShellElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-shell": UaiCopilotWorkspaceShellElement;
    }
}
