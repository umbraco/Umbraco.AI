import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { keyed } from "lit/directives/keyed.js";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolElementProps, UaiAgentToolStatus } from "./uai-agent-tool.extension.js";

/**
 * Default element for displaying tool call status.
 * Shows an icon, tool name, and loading indicator based on status.
 *
 * This is the default element used by the `uaiAgentTool` kind when no
 * custom element is specified.
 */
@customElement("uai-agent-tool-status")
export class UaiAgentToolStatusElement extends UmbLitElement implements UaiAgentToolElementProps {
  @property({ type: Object })
  args: Record<string, unknown> = {};

  @property({ type: String })
  status: UaiAgentToolStatus = "pending";

  @property({ type: Object })
  result?: unknown;

  /** Display name for the tool */
  @property({ type: String })
  name = "Tool";

  /** Icon to display */
  @property({ type: String })
  icon = "icon-wand";

  override render() {
    const statusIcon: Record<UaiAgentToolStatus, string> = {
      pending: "icon-hourglass",
      streaming: "icon-sync",
      awaiting_approval: "icon-user",
      executing: "icon-sync",
      complete: "icon-check",
      error: "icon-alert",
    };

    const isLoading = this.status === "streaming" || this.status === "executing";

    const iconName = statusIcon[this.status];

    return html`
      <div class="tool-status ${this.status}">
        ${keyed(iconName, html`<uui-icon name=${iconName}></uui-icon>`)}
        <span class="tool-name">${this.name}</span>
        ${isLoading ? html`<uui-loader-circle></uui-loader-circle>` : ""}
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: inline-block;
    }

    .tool-status {
      display: inline-flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-2) var(--uui-size-space-3);
      background: var(--uui-color-surface-alt);
      border-radius: var(--uui-border-radius);
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text);
    }

    .tool-status.complete {
      color: var(--uui-color-positive);
    }

    .tool-status.error {
      color: var(--uui-color-danger);
    }

    .tool-status.streaming uui-icon,
    .tool-status.executing uui-icon {
      animation: spin 1s linear infinite;
    }

    .tool-name {
      font-weight: 500;
    }

    uui-loader-circle {
      --uui-loader-circle-size: 14px;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }
  `;
}

export default UaiAgentToolStatusElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-agent-tool-status": UaiAgentToolStatusElement;
  }
}
