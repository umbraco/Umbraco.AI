import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";
import type { ManifestSection, UmbSectionElement } from "@umbraco-cms/backoffice/section";

import { UaiCopilotWorkspaceSidebarContext } from "../../sidebar/sidebar.context.js";

// Region elements (side-effect imports register the custom elements used in the template).
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

    constructor() {
        super();
        // Provides UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT (conversations/projects/search/active path) to
        // the sidebar apps rendered in the section-sidebar extension slot. Retained by the controller host.
        new UaiCopilotWorkspaceSidebarContext(this);
    }

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
            // Must precede "project/:id" so "create" isn't captured as an id.
            path: "project/create",
            component: () => import("../../project/workspace/project-workspace-editor.element.js"),
            setup: (component) => {
                (component as { create?: boolean }).create = true;
                this._activeProjectId = undefined;
                this._activeConversationId = undefined;
            },
        },
        {
            path: "project/:id",
            component: () => import("../../project/workspace/project-workspace-editor.element.js"),
            setup: (component, info) => {
                (component as { projectId?: string }).projectId = info.match.params.id;
                this._activeProjectId = info.match.params.id;
                this._activeConversationId = undefined;
            },
        },
        {
            path: "",
            component: () => import("./views/workspace-launcher.element.js"),
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
                    <umb-extension-slot type="sectionSidebarApp"></umb-extension-slot>
                </umb-section-sidebar>
                <div slot="end" class="main-area" style=${this.#mainAreaStyle()}>
                    <main>
                        <umb-router-slot .routes=${this._routes}></umb-router-slot>
                    </main>
                    ${this.#showContextPanel() ? this.#renderContextRegion() : nothing}
                </div>
            </umb-split-panel>
        `;
    }

    /** The context panel is a companion to an open conversation only (projects/launcher are full-width). */
    #showContextPanel() {
        return !!this._activeConversationId;
    }

    #mainAreaStyle() {
        if (!this.#showContextPanel()) return "grid-template-columns: minmax(0, 1fr);";
        const rightColumn = this._rightCollapsed ? "2.5rem" : `${this._rightWidth}px`;
        return `grid-template-columns: minmax(0, 1fr) ${rightColumn};`;
    }

    /**
     * The right region in both states. The open/close toggle is a single element anchored to the same
     * top-right spot regardless of state (`.right` is always flush to the screen's right edge and the
     * toggle is `position: absolute; top: 0; right: 0` with a fixed 2.5rem box), so its icon stays put
     * when you collapse/expand — only the chevron rotates. When collapsed the whole 2.5rem strip is a
     * click target with a clear hover; when expanded the panel and resizer render behind the toggle.
     */
    #renderContextRegion() {
        const collapsed = this._rightCollapsed;
        const label = collapsed
            ? this.localize.term("uaiCopilotWorkspace_contextExpand")
            : this.localize.term("uaiCopilotWorkspace_contextCollapse");
        return html`
            <div class="right ${collapsed ? "is-collapsed" : ""}">
                ${collapsed
                    ? html`<button
                          class="collapsed-strip"
                          tabindex="-1"
                          aria-hidden="true"
                          @click=${() => this.#setCollapsed(false)}
                      ></button>`
                    : html`
                          <div class="resizer" title="Drag to resize" @pointerdown=${this.#startResize}></div>
                          <uai-copilot-workspace-context-panel
                              .conversationId=${this._activeConversationId}
                              .projectId=${this._activeProjectId}
                          ></uai-copilot-workspace-context-panel>
                      `}
                <button
                    class="context-toggle"
                    title=${label}
                    aria-label=${label}
                    aria-expanded=${collapsed ? "false" : "true"}
                    @click=${() => this.#setCollapsed(!collapsed)}
                >
                    <uui-icon name="icon-navigation-right"></uui-icon>
                </button>
            </div>
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
                /* Surface background so the transparent resizer strip reads as white and sits flush
                   against the border — otherwise the page background shows through as a grey sliver
                   between the border and the panel body. */
                background: var(--uui-color-surface);
                /* Match the section sidebar's separator (uses --uui-color-border, not divider). */
                border-left: 1px solid var(--uui-color-border);
            }
            .right uai-copilot-workspace-context-panel {
                flex: 1;
                min-width: 0;
            }
            /* Overlay on the panel's left seam rather than a flex child — keeps the panel body (and
               its header/block dividers) flush to the border instead of inset by the handle width. */
            .resizer {
                position: absolute;
                left: 0;
                top: 0;
                bottom: 0;
                width: 6px;
                z-index: 2;
                cursor: col-resize;
                background: transparent;
                transition: background 120ms;
            }
            .resizer:hover {
                background: var(--uui-color-focus, #3879ff);
            }
            /* Full-height click target behind the toggle when collapsed, with a clearly
               distinct hover (surface-emphasis) so it doesn't blend into the chat surface. */
            .collapsed-strip {
                all: unset;
                flex: 1;
                height: 100%;
                cursor: pointer;
                background: var(--uui-color-surface);
            }
            .collapsed-strip:hover {
                background: var(--uui-color-surface-emphasis);
            }
            /* Single toggle, anchored to the same top-right spot in both states so its icon never
               jumps — only the chevron rotates. The 2.5rem box matches the collapsed column width,
               so the icon's centre lines up whether the panel is open or collapsed. */
            .context-toggle {
                all: unset;
                position: absolute;
                top: 0;
                right: 0;
                display: flex;
                align-items: center;
                justify-content: center;
                width: 2.5rem;
                height: var(--umb-header-layout-height);
                cursor: pointer;
                color: var(--uui-color-text-alt);
            }
            .context-toggle:hover {
                color: var(--uui-color-text);
            }
            .context-toggle uui-icon {
                transition: transform 120ms ease;
            }
            .right.is-collapsed .context-toggle uui-icon {
                transform: rotate(180deg);
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
