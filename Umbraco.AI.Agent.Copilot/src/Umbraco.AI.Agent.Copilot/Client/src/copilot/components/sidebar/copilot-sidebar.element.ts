import { customElement, state, ref, createRef } from "@umbraco-cms/backoffice/external/lit";
import { html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
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

    /** Soft threshold at which the length meter reads "full" and turns amber. Not a hard cap. */
    readonly #lengthSoftLimit = 50;

    @state() private _isOpen = false;
    @state() private _showContent = false;
    @state() private _entityName?: string;
    @state() private _messageCount = 0;

    constructor() {
        super();

        this.consumeContext(UAI_COPILOT_CONTEXT, async (context) => {
            if (context) {
                this.#copilotContext = context;

                // Track the item in context so the chat can frame itself around it (placeholder +
                // empty-state intro), reinforcing that the copilot edits the open item.
                this.observe(context.selectedEntity$, (entity) => {
                    this._entityName = entity?.name;
                });

                // Drive the clear button + length meter from the conversation length.
                this.observe(context.messages$, (messages) => {
                    this._messageCount = messages.length;
                });

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

    #handleClear() {
        this.#copilotContext?.clearChat();
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
        // Frame the chat around the item in context when one is known; otherwise let uai-chat use its
        // own generic empty-state fallback.
        const placeholder = this._entityName
            ? this.localize.term("uaiCopilot_inputPlaceholder", this._entityName)
            : undefined;
        const introHeading = this._entityName
            ? this.localize.term("uaiCopilot_introHeading", this._entityName)
            : undefined;
        const introMessage = this.localize.term("uaiCopilot_introMessage");
        // Subtle, non-textual "chat is getting long" signal: a thin bar that fills toward a soft
        // threshold and shifts to a warning tint near it. No hard cap, no auto-trimming.
        const lengthRatio = Math.min(this._messageCount / this.#lengthSoftLimit, 1);
        const lengthWarn = lengthRatio >= 0.8;
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
                        <div class="header-actions">
                            ${this._messageCount > 0
                                ? html`<uui-button
                                      compact
                                      look="default"
                                      label=${this.localize.term("uaiCopilot_clearLabel")}
                                      @click=${this.#handleClear}>
                                      <uui-icon name="icon-trash"></uui-icon>
                                  </uui-button>`
                                : nothing}
                            <uui-button
                                compact
                                look="default"
                                label=${this.localize.term("uaiCopilot_closeLabel")}
                                @click=${this.#handleClose}>
                                <uui-icon name="icon-delete"></uui-icon>
                            </uui-button>
                        </div>
                    </header>
                    ${this._messageCount > 0
                        ? html`<div
                              class="length-meter ${lengthWarn ? "warn" : ""}"
                              title=${this.localize.term("uaiCopilot_lengthMeterTitle")}>
                              <div class="length-meter-fill" style="width:${Math.round(lengthRatio * 100)}%"></div>
                          </div>`
                        : nothing}
                    <uai-entity-selector></uai-entity-selector>
                    <div class="sidebar-content">
                        <uai-chat placeholder=${placeholder ?? nothing}>
                            ${introHeading
                                ? html`<div slot="empty-state-message" class="copilot-intro">
                                          <uui-icon name="icon-chat"></uui-icon>
                                          <h3>${introHeading}</h3>
                                          <p>${introMessage}</p>
                                      </div>`
                                : nothing}
                        </uai-chat>
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
        .header-actions {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-1);
        }

        /* Length meter: a thin capacity bar under the header. Subtle by default, amber near the
           soft threshold. Purely indicative — no text, no hard limit. */
        .length-meter {
            height: 2px;
            width: 100%;
            background: var(--uui-color-surface-alt);
            overflow: hidden;
        }
        .length-meter-fill {
            height: 100%;
            background: var(--uui-color-default);
            opacity: 0.35;
            transition:
                width 0.3s ease,
                background-color 0.3s ease,
                opacity 0.3s ease;
        }
        .length-meter.warn .length-meter-fill {
            background: var(--uui-color-warning-standalone, #f5c1bb);
            opacity: 0.9;
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

        /* Rich empty-state content projected into <uai-chat>'s "empty-state-message" slot: a friendly,
           context-named greeting in place of the generic prompt. */
        .copilot-intro {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            text-align: center;
            gap: var(--uui-size-space-3);
            color: var(--uui-color-text-alt);
        }
        .copilot-intro uui-icon {
            font-size: 40px;
            opacity: 0.5;
        }
        .copilot-intro h3 {
            margin: 0;
            font-size: 1.05rem;
            color: var(--uui-color-text);
        }
        .copilot-intro p {
            margin: 0;
            max-width: 32ch;
            font-size: var(--uui-type-default-size);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-sidebar": UaiCopilotSidebarElement;
    }
}
