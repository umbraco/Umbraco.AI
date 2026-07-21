import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";
import type { ManifestSection, UmbSectionElement } from "@umbraco-cms/backoffice/section";

// Region elements (side-effect imports register the custom elements used in the template).
import "../sidebar/workspace-conversation-list.element.js";
import "./regions/workspace-context-panel.element.js";

const RIGHT_MIN = 260;
const RIGHT_MAX = 640;
const RIGHT_DEFAULT = 340;
const STORAGE_WIDTH = "uai-cw-context-width";
const STORAGE_COLLAPSED = "uai-cw-context-collapsed";
const STORAGE_SIDEBAR = "uai-cw-sidebar-position";
const SIDEBAR_DEFAULT = "300px";

/**
 * The Copilot Workspace section element — a **standalone, fully-owned section** (deliberately NOT a
 * dashboard, so third parties can't register content into it and it doesn't inherit the section
 * dashboard/tab machinery). It renders all three regions itself:
 *  - left: the conversation list, in the standard `<umb-section-sidebar>` chrome (resizable via
 *    `<umb-split-panel>`);
 *  - center: an `<umb-router-slot>` for the section's routes — empty landing, an open conversation
 *    (`conversation/:id`), and a project (`project/:id`);
 *  - right: a collapsible + resizable context panel.
 *
 * Mounted directly at the section path (`/section/copilot-workspace`), so the center router bases
 * there and its routes resolve to `/section/copilot-workspace/{conversation|project}/:id`.
 */
@customElement("uai-copilot-workspace-shell")
export class UaiCopilotWorkspaceShellElement extends UmbLitElement implements UmbSectionElement {
    /** Required by {@link UmbSectionElement}; set by the extension host, otherwise unused. */
    public manifest?: ManifestSection;

    @state()
    private _sidebarPosition = readString(STORAGE_SIDEBAR, SIDEBAR_DEFAULT);

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

    #onSidebarPositionChanged = (event: CustomEvent) => {
        const position = String(event.detail?.position ?? this._sidebarPosition);
        this._sidebarPosition = position;
        writeStorage(STORAGE_SIDEBAR, position);
    };

    #setCollapsed(collapsed: boolean) {
        this._rightCollapsed = collapsed;
        writeStorage(STORAGE_COLLAPSED, String(collapsed));
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
            writeStorage(STORAGE_WIDTH, String(this._rightWidth));
        };
        window.addEventListener("pointermove", onMove);
        window.addEventListener("pointerup", onUp);
    };

    override render() {
        return html`
            <umb-split-panel
                lock="start"
                snap="300px"
                .position=${this._sidebarPosition}
                @position-changed=${this.#onSidebarPositionChanged}
            >
                <umb-section-sidebar slot="start">
                    <uai-copilot-workspace-conversation-list></uai-copilot-workspace-conversation-list>
                </umb-section-sidebar>
                <div slot="end" class="main-area" style=${this.#mainAreaStyle()}>
                    <main>
                        <umb-router-slot .routes=${this._routes}></umb-router-slot>
                    </main>
                    ${this._rightCollapsed ? this.#renderCollapsedStrip() : this.#renderExpandedPanel()}
                </div>
            </umb-split-panel>
        `;
    }

    #mainAreaStyle() {
        const rightColumn = this._rightCollapsed ? "2.5rem" : `${this._rightWidth}px`;
        return `grid-template-columns: minmax(0, 1fr) ${rightColumn};`;
    }

    #renderExpandedPanel() {
        return html`
            <div class="right">
                <div class="resizer" title="Drag to resize" @pointerdown=${this.#startResize}></div>
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
            .main-area {
                display: grid;
                height: 100%;
                min-height: 0;
                min-width: 0;
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

function readString(key: string, fallback: string): string {
    try {
        return localStorage.getItem(key) ?? fallback;
    } catch {
        return fallback;
    }
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
