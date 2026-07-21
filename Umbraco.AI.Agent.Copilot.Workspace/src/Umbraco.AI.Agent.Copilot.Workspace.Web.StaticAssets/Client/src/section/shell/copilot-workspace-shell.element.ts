import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";

// Region elements (side-effect imports register the custom elements used in the template).
// The conversation list lives in the CMS section sidebar (a sectionSidebarApp), not here.
import "./regions/workspace-context-panel.element.js";

const RIGHT_MIN = 260;
const RIGHT_MAX = 640;
const RIGHT_DEFAULT = 340;
const STORAGE_WIDTH = "uai-cw-context-width";
const STORAGE_COLLAPSED = "uai-cw-context-collapsed";

/**
 * The Copilot Workspace main-area shell: a routed main area plus a collapsible + resizable context
 * panel (right). The conversation list is the CMS section sidebar (a sectionSidebarApp), giving the
 * standard sidebar chrome. The center hosts an <umb-router-slot> with the section's routes: empty
 * landing, an open conversation (/conversation/:id), and a project (/project/:id).
 */
@customElement("uai-copilot-workspace-shell")
export class UaiCopilotWorkspaceShellElement extends UmbLitElement {
    @state()
    private _rightWidth = readNumber(STORAGE_WIDTH, RIGHT_DEFAULT);

    @state()
    private _rightCollapsed = readBool(STORAGE_COLLAPSED, false);

    /** Active conversation id (conversation route), passed to the context panel. */
    @state()
    private _activeConversationId?: string;

    /** Active project id (project route), passed to the context panel. */
    @state()
    private _activeProjectId?: string;

    @state()
    private _routes: UmbRoute[] = [
        {
            path: "conversation/:id",
            component: () => import("./views/workspace-conversation-view.element.js"),
            setup: (component, info) => {
                (component as { conversationId?: string }).conversationId = info.match.params.id;
                this._activeConversationId = info.match.params.id;
                this._activeProjectId = undefined;
            },
        },
        {
            path: "project/:id",
            component: () => import("./views/workspace-project-view.element.js"),
            setup: (component, info) => {
                (component as { projectId?: string }).projectId = info.match.params.id;
                this._activeProjectId = info.match.params.id;
                this._activeConversationId = undefined;
            },
        },
        {
            path: "",
            component: () => import("./views/workspace-empty-view.element.js"),
            setup: () => {
                this._activeConversationId = undefined;
                this._activeProjectId = undefined;
            },
        },
        {
            path: "**",
            component: async () => (await import("@umbraco-cms/backoffice/router")).UmbRouteNotFoundElement,
        },
    ];

    #setCollapsed(collapsed: boolean) {
        this._rightCollapsed = collapsed;
        try {
            localStorage.setItem(STORAGE_COLLAPSED, String(collapsed));
        } catch {
            /* storage unavailable — in-session only */
        }
    }

    #startResize = (event: PointerEvent) => {
        event.preventDefault();
        const startX = event.clientX;
        const startWidth = this._rightWidth;
        document.body.style.userSelect = "none";

        const onMove = (moveEvent: PointerEvent) => {
            // Dragging the handle left widens the panel (it sits on the panel's left edge).
            const next = startWidth + (startX - moveEvent.clientX);
            this._rightWidth = Math.min(RIGHT_MAX, Math.max(RIGHT_MIN, next));
        };
        const onUp = () => {
            window.removeEventListener("pointermove", onMove);
            window.removeEventListener("pointerup", onUp);
            document.body.style.userSelect = "";
            try {
                localStorage.setItem(STORAGE_WIDTH, String(this._rightWidth));
            } catch {
                /* storage unavailable */
            }
        };
        window.addEventListener("pointermove", onMove);
        window.addEventListener("pointerup", onUp);
    };

    override render() {
        const rightColumn = this._rightCollapsed ? "2.5rem" : `${this._rightWidth}px`;
        const style = `grid-template-columns: minmax(0, 1fr) ${rightColumn};`;

        return html`
            <div class="shell" style=${style}>
                <main>
                    <umb-router-slot .routes=${this._routes}></umb-router-slot>
                </main>
                ${this._rightCollapsed ? this.#renderCollapsedStrip() : this.#renderExpandedPanel()}
            </div>
        `;
    }

    #renderExpandedPanel() {
        return html`
            <div class="right">
                <div
                    class="resizer"
                    title="Drag to resize"
                    @pointerdown=${this.#startResize}
                ></div>
                <uai-copilot-workspace-context-panel
                    .conversationId=${this._activeConversationId}
                    .projectId=${this._activeProjectId}
                    @collapse=${() => this.#setCollapsed(true)}
                ></uai-copilot-workspace-context-panel>
            </div>
        `;
    }

    #renderCollapsedStrip() {
        return html`
            <button
                class="expand-strip"
                title="Show context panel"
                aria-label="Show context panel"
                @click=${() => this.#setCollapsed(false)}
            >
                <uui-icon name="icon-navigation-left"></uui-icon>
            </button>
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
                height: 100%;
                min-height: 0;
            }
            main {
                min-width: 0;
                height: 100%;
                overflow: hidden;
            }
            .right {
                position: relative;
                display: flex;
                height: 100%;
                min-height: 0;
                border-left: 1px solid var(--uui-color-divider);
            }
            .right uai-copilot-workspace-context-panel {
                flex: 1;
                min-width: 0;
            }
            .resizer {
                flex: 0 0 6px;
                cursor: col-resize;
                background: transparent;
                transition: background 120ms;
            }
            .resizer:hover {
                background: var(--uui-color-focus, #3879ff);
            }
            .expand-strip {
                all: unset;
                display: flex;
                align-items: flex-start;
                justify-content: center;
                padding-top: var(--uui-size-space-3);
                height: 100%;
                cursor: pointer;
                border-left: 1px solid var(--uui-color-divider);
                background: var(--uui-color-surface);
                color: var(--uui-color-text-alt);
            }
            .expand-strip:hover {
                background: var(--uui-color-surface-alt);
                color: var(--uui-color-text);
            }
        `,
    ];
}

function readNumber(key: string, fallback: number): number {
    try {
        const raw = localStorage.getItem(key);
        const value = raw ? Number(raw) : NaN;
        return Number.isFinite(value) ? value : fallback;
    } catch {
        return fallback;
    }
}

function readBool(key: string, fallback: boolean): boolean {
    try {
        const raw = localStorage.getItem(key);
        return raw === null ? fallback : raw === "true";
    } catch {
        return fallback;
    }
}

export default UaiCopilotWorkspaceShellElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-shell": UaiCopilotWorkspaceShellElement;
    }
}
