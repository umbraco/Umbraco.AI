import { customElement, state, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_CONTEXT, type UaiCopilotContext } from "../../copilot.context.js";
import { agentClientReady } from "@umbraco-ai/agent";

/** Shell sidebar that binds layout controls to the Copilot context. */
@customElement("uai-copilot-sidebar")
export class UaiCopilotSidebarElement extends UmbLitElement {
    #copilotContext?: UaiCopilotContext;
    #panelRef = createRef<HTMLElement>();

    readonly #sidebarWidth = 450;

    #updateContentOffset(isOpen: boolean) {
        // Apply margin to body element to push entire page content
        document.body.style.marginInlineEnd = isOpen ? `${this.#sidebarWidth}px` : "";
        document.body.style.transition = "margin-inline-end 0.3s ease";
    }

    @state() private _isOpen = false;
    @state() private _showContent = false;

    constructor() {
        super();

        this.consumeContext(UAI_COPILOT_CONTEXT, async (context) => {
            if (context) {
                this.#copilotContext = context;
                this.observe(context.isOpen, (isOpen) => {
                    console.debug(`Copilot Sidebar is now ${isOpen ? "open" : "closed"}`);
                    this._isOpen = isOpen;
                    this.#updateContentOffset(isOpen);

                    // Show content immediately on open; on close, wait for slide-out transition
                    if (isOpen) {
                        this._showContent = true;
                        // Move focus into the panel once its content has rendered. The chat input
                        // self-focuses when the run controller enables it; focusing the panel here is
                        // the reliable floor so keyboard/screen-reader users land inside the panel
                        // (and ESC has somewhere to fire from) even if that doesn't happen.
                        this.updateComplete.then(() => this.#panelRef.value?.focus());
                    }
                });

                // Auto-close when navigating away from a supported workspace. Shares the copilot's
                // debounced support signal with the FAB, so supported⇄supported hops neither hide the
                // button nor close the panel (which would reset the conversation).
                this.observe(
                    context.isSupportedWorkspace$,
                    (supported) => {
                        if (!supported && this._isOpen) {
                            context.close();
                        }
                    },
                    "_observeSupportedWorkspace"
                );

                // Wait for agent package's client to be configured before loading agents
                await agentClientReady;
                context.loadAgents();
            }
        });
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#updateContentOffset(false); // Reset margin when component unmounts
    }

    #handleClose() {
        this.#copilotContext?.close();
    }

    #handleKeydown(e: KeyboardEvent) {
        // ESC closes the panel. keydown is composed, so this fires even when focus is inside the chat
        // input (a separate shadow tree). Stop propagation so it doesn't also trigger unrelated
        // backoffice Escape handling. Focus returns to the button via UaiCopilotFabController.
        if (e.key === "Escape" && this._isOpen) {
            e.stopPropagation();
            this.#copilotContext?.close();
        }
    }

    #handleTransitionEnd(e: TransitionEvent) {
        // Only react to the sidebar's own transform transition
        if (e.propertyName === "transform" && !this._isOpen) {
            this._showContent = false;
        }
    }

    override render() {
        const title = this.localize.term("uaiCopilot_sidebarTitle");
        return html`
            <aside
                ${ref(this.#panelRef)}
                class="sidebar ${this._isOpen ? "open" : ""}"
                role="complementary"
                aria-label=${title}
                tabindex="-1"
                @keydown=${this.#handleKeydown}
                @transitionend=${this.#handleTransitionEnd}>
                ${this._showContent ? html`
                    <header class="sidebar-header">
                        <div class="header-content">
                            <h3 class="header-title">${title}</h3>
                        </div>
                        <uui-button
                            compact
                            look="default"
                            label=${this.localize.term("uaiCopilot_closeLabel")}
                            @click=${this.#handleClose}>
                            <uui-icon name="icon-delete"></uui-icon>
                        </uui-button>
                    </header>
                    <uai-entity-selector></uai-entity-selector>
                    <div class="sidebar-content">
                        <uai-chat></uai-chat>
                    </div>
                ` : ''}
            </aside>
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }

        .sidebar {
            position: fixed;
            top: 0;
            right: 0;
            bottom: 0;
            width: 450px;
            max-width: 90vw;
            background: var(--uui-color-surface);
            border-left: 1px solid var(--uui-color-border);
            transform: translateX(100%);
            transition: transform 0.3s ease;
            z-index: 1000;
            display: flex;
            flex-direction: column;
        }
        .sidebar.open {
            transform: translateX(0);
        }

        .sidebar-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: var(--uui-size-space-4) var(--uui-size-space-4) var(--uui-size-space-4) var(--uui-size-space-5);
            border-bottom: 1px solid var(--uui-color-border);
            background: var(--uui-color-surface-alt);
            height: 60px;
            box-sizing: border-box;
        }
        .header-content {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
        }
        .header-title {
            font-weight: 600;
        }

        .sidebar-content {
            flex: 1;
            overflow: hidden;
            display: flex;
            flex-direction: column;
        }

        uui-icon {
            font-size: 16px;
        }

        uai-chat {
            flex: 1;
            display: block;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-sidebar": UaiCopilotSidebarElement;
    }
}
