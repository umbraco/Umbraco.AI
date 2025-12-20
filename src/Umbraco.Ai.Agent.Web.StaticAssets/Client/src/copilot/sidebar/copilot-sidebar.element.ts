import { customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbExtensionRegistry, ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import { UmbCopilotSidebarContext } from "./copilot-sidebar.context.js";
import { UaiCopilotRepository, type CopilotAgentItem } from "./copilot.repository.js";

// Import registers the React chat web component
import "../web-components/index.js";

@customElement("uai-copilot-sidebar")
export class UaiCopilotSidebarElement extends UmbLitElement {
  // Provide global context
  #sidebarContext = new UmbCopilotSidebarContext(this);
  #repository = new UaiCopilotRepository(this);

  @state() private _isOpen = false;
  @state() private _agents: CopilotAgentItem[] = [];
  @state() private _selectedAgentId = "";
  @state() private _selectedAgentName = "";
  @state() private _loading = true;

  constructor() {
    super();
    this.observe(this.#sidebarContext.isOpen, (isOpen) => (this._isOpen = isOpen));
    this.observe(this.#sidebarContext.agentId, (id) => (this._selectedAgentId = id));
    this.observe(this.#sidebarContext.agentName, (name) => (this._selectedAgentName = name));
  }

  override connectedCallback() {
    super.connectedCallback();
    this.#loadAgents();
  }

  async #loadAgents() {
    const { data, error } = await this.#repository.requestActiveAgents();

    if (error) {
      console.error("Failed to load agents:", error);
      this._loading = false;
      return;
    }

    if (data) {
      this._agents = data;
      // Auto-select first agent if none selected
      if (!this._selectedAgentId && this._agents.length > 0) {
        this.#sidebarContext.setAgent(this._agents[0].id, this._agents[0].name);
      }
    }

    this._loading = false;
  }

  #handleAgentChange(e: Event) {
    const select = e.target as HTMLSelectElement;
    const agent = this._agents.find((a) => a.id === select.value);
    if (agent) {
      this.#sidebarContext.setAgent(agent.id, agent.name);
    }
  }

  #handleClose() {
    this.#sidebarContext.close();
  }

  override render() {
    return html`
      <div class="sidebar-overlay ${this._isOpen ? "open" : ""}" @click=${this.#handleClose}></div>
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
          ${this._selectedAgentId
            ? html`
                <uai-copilot-chat agentId=${this._selectedAgentId} agentName=${this._selectedAgentName}>
                </uai-copilot-chat>
              `
            : html` <div class="no-agent">Select an agent to start chatting</div> `}
        </div>
      </aside>
    `;
  }

  static override styles = css`
    :host {
      display: contents;
    }

    .sidebar-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.3);
      opacity: 0;
      pointer-events: none;
      transition: opacity 0.2s ease;
      z-index: 999;
    }
    .sidebar-overlay.open {
      opacity: 1;
      pointer-events: auto;
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

export const onInit = (_host: UmbControllerHost, _extensionRegistry: UmbExtensionRegistry<ManifestBase>): void => {
  // Sidebar element self-registers via @customElement decorator
  console.log("Copilot sidebar entry point initialized");
};

export const onUnload = (_host: UmbControllerHost, _extensionRegistry: UmbExtensionRegistry<ManifestBase>): void => {
  // Cleanup if needed
};

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-sidebar": UaiCopilotSidebarElement;
  }
}
