import { customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { CopilotAgentItem } from "./copilot.repository.js";
import { UMB_COPILOT_CONTEXT, type UmbCopilotContext } from "../../core/copilot.context.js";

// Import native chat component (lightweight, no lazy loading needed)
import "../chat/index.js";

/** Shell sidebar that binds layout controls to the Copilot context. */
@customElement("uai-copilot-sidebar")
export class UaiCopilotSidebarElement extends UmbLitElement {
  #copilotContext?: UmbCopilotContext;

  readonly #sidebarWidth = 400;

  #updateContentOffset(isOpen: boolean) {
    // Apply margin to body element to push entire page content
    document.body.style.marginInlineEnd = isOpen ? `${this.#sidebarWidth}px` : "";
    document.body.style.transition = "margin-inline-end 0.3s ease";
  }

  @state() private _isOpen = false;
  @state() private _agents: CopilotAgentItem[] = [];
  @state() private _selectedAgentId = "";
  @state() private _selectedAgentName = "";
  @state() private _loading = true;

  constructor() {
    super();
    this.consumeContext(UMB_COPILOT_CONTEXT, (context) => {
      if (context) {
        this.#copilotContext = context;
        this.#observeCopilotContext();
      }
    });
  }

  #observeCopilotContext() {
    if (!this.#copilotContext) return;
    this.observe(this.#copilotContext.isOpen, (isOpen) => {
      this._isOpen = isOpen;
      this.#updateContentOffset(isOpen);
    });
    this.observe(this.#copilotContext.agentId, (id) => (this._selectedAgentId = id));
    this.observe(this.#copilotContext.agentName, (name) => (this._selectedAgentName = name));
    this.observe(this.#copilotContext.agents, (agents) => (this._agents = agents));
    this.observe(this.#copilotContext.agentsLoading, (loading) => (this._loading = loading));
  }

  override connectedCallback() {
    super.connectedCallback();
    this.#copilotContext?.loadAgents();
  }

  override disconnectedCallback() {
    super.disconnectedCallback();
    this.#updateContentOffset(false); // Reset margin when component unmounts
  }

  #handleAgentChange(e: Event) {
    const select = e.target as HTMLSelectElement;
    const agent = this._agents.find((a) => a.id === select.value);
    if (agent) {
      this.#copilotContext?.setAgent(agent.id);
    }
  }

  #handleClose() {
    this.#copilotContext?.close();
  }

  #renderChatContent() {
    if (this._selectedAgentId) {
      return html`
        <uai-copilot-chat agentId=${this._selectedAgentId} agentName=${this._selectedAgentName}>
        </uai-copilot-chat>
      `;
    }
    return html`<div class="no-agent">Select an agent to start chatting</div>`;
  }

  override render() {
    return html`
      <aside class="sidebar ${this._isOpen ? "open" : ""}">
        <header class="sidebar-header">
          <div class="header-content">
            <uui-icon name="icon-chat"></uui-icon>
            ${this._loading
              ? html`<span>Loading...</span>`
              : html`
                  <uui-select .value=${this._selectedAgentId} @change=${this.#handleAgentChange}>
                    ${this._agents.map(
                      (agent) => html`
                        <uui-select-option value=${agent.id} ?selected=${agent.id === this._selectedAgentId}>
                          ${agent.name}
                        </uui-select-option>
                      `
                    )}
                  </uui-select>
                `}
          </div>
          <uui-button compact look="default" @click=${this.#handleClose}>
            <uui-icon name="icon-wrong"></uui-icon>
          </uui-button>
        </header>
        <div class="sidebar-content">
          ${this.#renderChatContent()}
        </div>
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
      width: 400px;
      max-width: 90vw;
      background: var(--uui-color-surface);
      box-shadow: var(--uui-shadow-depth-3);
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
      padding: var(--uui-size-space-4);
      border-bottom: 1px solid var(--uui-color-border);
      background: var(--uui-color-surface-alt);
    }
    .header-content {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }
    .header-content uui-select {
      min-width: 200px;
    }

    .sidebar-content {
      flex: 1;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }

    uai-copilot-chat {
      flex: 1;
      display: block;
    }

    .no-agent {
      padding: var(--uui-size-space-5);
      text-align: center;
      color: var(--uui-color-text-alt);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-sidebar": UaiCopilotSidebarElement;
  }
}
