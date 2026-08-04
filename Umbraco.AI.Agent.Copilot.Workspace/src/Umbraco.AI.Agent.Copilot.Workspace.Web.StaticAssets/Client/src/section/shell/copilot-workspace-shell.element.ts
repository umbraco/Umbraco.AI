import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";
import type { ManifestSection, UmbSectionElement } from "@umbraco-cms/backoffice/section";

import { UaiCopilotWorkspaceSidebarContext } from "../../sidebar/sidebar.context.js";

const STORAGE_MENU = "uai-cw-sidebar-position";
const MENU_DEFAULT = "300px";

/**
 * The Copilot Workspace section element — thin section chrome: the **menu area** (the conversation list,
 * in the standard `<umb-section-sidebar>`, resizable via `<umb-split-panel>`) and an `<umb-router-slot>`
 * for the section's routes (launcher, a conversation, a project). Deliberately NOT a dashboard, so third
 * parties can't register content into it.
 *
 * Everything conversation-specific — the chat, the context panel, and the slide-out panel mechanics —
 * lives in the conversation workspace mounted by the `conversation/*` routes; the shell knows nothing
 * about it.
 */
@customElement("uai-copilot-workspace-shell")
export class UaiCopilotWorkspaceShellElement extends UmbLitElement implements UmbSectionElement {
    /** Required by {@link UmbSectionElement}; set by the extension host, otherwise unused. */
    public manifest?: ManifestSection;

    constructor() {
        super();
        // Provides UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT (conversations/projects/search/active path) to
        // the menu-area apps rendered in the section-sidebar extension slot. Retained by the controller host.
        new UaiCopilotWorkspaceSidebarContext(this);
    }

    @state()
    private _menuPosition = readString(STORAGE_MENU, MENU_DEFAULT);

    @state()
    private _routes: UmbRoute[] = [
        {
            // Must precede "conversation/:id" so "create" isn't captured as an id. Starts a draft — no
            // conversation is persisted until the first message is sent (see the chat context).
            path: "conversation/create",
            component: () => import("../../conversation/workspace/conversation-workspace.element.js"),
            setup: (component) => {
                const projectId = new URLSearchParams(window.location.search).get("projectId") ?? undefined;
                (component as { startDraft?: (projectId?: string) => void }).startDraft?.(projectId);
            },
        },
        {
            path: "conversation/:id",
            component: () => import("../../conversation/workspace/conversation-workspace.element.js"),
            setup: (component, info) => {
                (component as { conversationId?: string }).conversationId = info.match.params.id;
            },
        },
        {
            // Must precede "project/:id" so "create" isn't captured as an id.
            path: "project/create",
            component: () => import("../../project/workspace/project-workspace-editor.element.js"),
            setup: (component) => {
                (component as { create?: boolean }).create = true;
            },
        },
        {
            path: "project/:id",
            component: () => import("../../project/workspace/project-workspace-editor.element.js"),
            setup: (component, info) => {
                (component as { projectId?: string }).projectId = info.match.params.id;
            },
        },
        {
            path: "",
            component: () => import("./views/workspace-launcher.element.js"),
        },
        {
            path: "**",
            component: async () => (await import("@umbraco-cms/backoffice/router")).UmbRouteNotFoundElement,
        },
    ];

    #onMenuPositionChanged = (event: CustomEvent) => {
        const position = String(event.detail?.position ?? this._menuPosition);
        this._menuPosition = position;
        writeStorage(STORAGE_MENU, position);
    };

    override render() {
        return html`
            <umb-split-panel
                lock="start"
                snap="300px"
                .position=${this._menuPosition}
                @position-changed=${this.#onMenuPositionChanged}
            >
                <umb-section-sidebar slot="start">
                    <umb-extension-slot type="sectionSidebarApp"></umb-extension-slot>
                </umb-section-sidebar>
                <div slot="end" class="main">
                    <umb-router-slot .routes=${this._routes}></umb-router-slot>
                </div>
            </umb-split-panel>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex: 1 1 auto;
                height: 100%;
                min-height: 0;
            }
            umb-split-panel {
                width: 100%;
                height: 100%;
                --umb-split-panel-start-min-width: 240px;
                --umb-split-panel-start-max-width: 460px;
            }
            umb-section-sidebar {
                height: 100%;
            }
            .main {
                height: 100%;
                min-width: 0;
                overflow: hidden;
            }
        `,
    ];
}

function readString(key: string, fallback: string): string {
    try {
        return localStorage.getItem(key) ?? fallback;
    } catch {
        return fallback;
    }
}

function writeStorage(key: string, value: string): void {
    try {
        localStorage.setItem(key, value);
    } catch {
        /* storage unavailable — in-session only */
    }
}

export default UaiCopilotWorkspaceShellElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-shell": UaiCopilotWorkspaceShellElement;
    }
}
