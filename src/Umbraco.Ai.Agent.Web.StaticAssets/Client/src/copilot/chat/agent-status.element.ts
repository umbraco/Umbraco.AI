import { customElement, property, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { AgentState } from "./types.js";

/**
 * Agent status component.
 * Shows agent thinking/progress state.
 */
@customElement("uai-copilot-agent-status")
export class UaiCopilotAgentStatusElement extends UmbLitElement {
  @property({ type: Object })
  state?: AgentState;

  #getDefaultLabel(): string {
    const labels: Record<AgentState["status"], string> = {
      thinking: "Thinking...",
      executing: "Executing...",
      awaiting_input: "Waiting for input...",
      idle: "",
    };
    return labels[this.state?.status ?? "idle"];
  }

  #renderProgress() {
    if (!this.state?.progress) {
      return nothing;
    }

    const { current, total, label } = this.state.progress;
    const percentage = total > 0 ? (current / total) * 100 : 0;

    return html`
      <div class="progress">
        <div class="progress-bar">
          <div class="progress-fill" style="width: ${percentage}%"></div>
        </div>
        ${label ? html`<span class="progress-label">${label}</span>` : nothing}
      </div>
    `;
  }

  override render() {
    if (!this.state || this.state.status === "idle") {
      return nothing;
    }

    const showLoader =
      this.state.status === "thinking" || this.state.status === "executing";

    return html`
      <div class="agent-status">
        ${showLoader ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
        <span class="status-text">${this.state.currentStep ?? this.#getDefaultLabel()}</span>
        ${this.#renderProgress()}
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .agent-status {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-2) var(--uui-size-space-3);
      background: var(--uui-color-surface-alt);
      color: var(--uui-color-text-alt);
      font-size: var(--uui-type-small-size);
      border-radius: var(--uui-border-radius);
      margin: var(--uui-size-space-2) var(--uui-size-space-3);
    }

    uui-loader-circle {
      --uui-loader-circle-size: 16px;
    }

    .status-text {
      flex-shrink: 0;
    }

    .progress {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
    }

    .progress-bar {
      height: 4px;
      background: var(--uui-color-border);
      border-radius: 2px;
      overflow: hidden;
    }

    .progress-fill {
      height: 100%;
      background: var(--uui-color-positive);
      transition: width 0.3s ease;
    }

    .progress-label {
      font-size: var(--uui-type-small-size);
      opacity: 0.8;
    }
  `;
}

export default UaiCopilotAgentStatusElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-agent-status": UaiCopilotAgentStatusElement;
  }
}
