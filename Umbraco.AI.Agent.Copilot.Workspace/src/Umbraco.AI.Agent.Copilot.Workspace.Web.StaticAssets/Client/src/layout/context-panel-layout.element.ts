import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

const RIGHT_MIN = 260;
const RIGHT_MAX = 640;
const RIGHT_DEFAULT = 340;
const STORAGE_WIDTH = "uai-cw-context-width";
const STORAGE_COLLAPSED = "uai-cw-context-collapsed";

/**
 * Reusable two-region layout: a `main` slot and a collapsible + resizable right `aside` slot. Owns all
 * the slide-out chrome — the resize handle, the collapse/expand toggle, and persistence of the width and
 * collapsed state — so the consumer (the conversation workspace) just supplies slot contents and stays
 * ignorant of the panel mechanics. Domain-agnostic.
 */
@customElement("uai-copilot-workspace-context-panel-layout")
export class UaiCopilotWorkspaceContextPanelLayoutElement extends UmbLitElement {
    @state() private _rightWidth = readNumber(STORAGE_WIDTH, RIGHT_DEFAULT);
    @state() private _rightCollapsed = readBool(STORAGE_COLLAPSED, false);

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
        const rightColumn = this._rightCollapsed ? "2.5rem" : `${this._rightWidth}px`;
        return html`
            <div class="main-area" style="grid-template-columns: minmax(0, 1fr) ${rightColumn};">
                <main><slot name="main"></slot></main>
                ${this.#renderRight()}
            </div>
        `;
    }

    /**
     * The right region in both states. The open/close toggle is anchored to the same top-right spot
     * regardless of state so its icon stays put when you collapse/expand — only the chevron rotates.
     */
    #renderRight() {
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
                          <slot name="aside"></slot>
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
                display: block;
                height: 100%;
                min-height: 0;
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
                   against the border — otherwise the page background shows through as a grey sliver. */
                background: var(--uui-color-surface);
                border-left: 1px solid var(--uui-color-border);
            }
            .right ::slotted(*) {
                flex: 1;
                min-width: 0;
            }
            /* Overlay on the panel's left seam rather than a flex child — keeps the panel body flush to
               the border instead of inset by the handle width. */
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

export default UaiCopilotWorkspaceContextPanelLayoutElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-context-panel-layout": UaiCopilotWorkspaceContextPanelLayoutElement;
    }
}
