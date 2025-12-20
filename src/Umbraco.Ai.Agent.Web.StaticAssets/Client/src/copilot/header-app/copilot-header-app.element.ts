import { customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_COPILOT_SIDEBAR_CONTEXT, type UmbCopilotSidebarContext } from "../sidebar/copilot-sidebar.context.js";

@customElement("uai-copilot-header-app")
export class UaiCopilotHeaderAppElement extends UmbLitElement {
  #sidebarContext?: UmbCopilotSidebarContext;
  @state() private _isOpen = false;

  constructor() {
    super();
    this.consumeContext(UMB_COPILOT_SIDEBAR_CONTEXT, (context) => {
      this.#sidebarContext = context;
      if (context) {
        this.observe(context.isOpen, (isOpen) => (this._isOpen = isOpen));
      }
    });
  }

  #handleClick() {
    this.#sidebarContext?.toggle();
  }

  override render() {
    return html`
      <uui-button
        look="primary"
        label="AI Assistant"
        compact
        @click=${this.#handleClick}
        class=${this._isOpen ? "active" : ""}>
        <uui-icon name="icon-chat"></uui-icon>
      </uui-button>
    `;
  }

  static override styles = css`
    :host {
      display: flex;
      align-items: center;
    }
    uui-button.active {
      background-color: var(--uui-color-selected);
    }
  `;
}

export default UaiCopilotHeaderAppElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-header-app": UaiCopilotHeaderAppElement;
  }
}
