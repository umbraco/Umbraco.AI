import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolElementProps } from "./uai-agent-tool.extension.js";

/**
 * Tool call information passed to the element.
 */
export interface ToolCallInfo {
  id: string;
  name: string;
  arguments: string;
  result?: string;
  status: "pending" | "running" | "completed" | "error";
}

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
  status: "pending" | "running" | "completed" | "error" = "pending";

  @property({ type: Object })
  result?: unknown;

  /**
   * Alternative property for when used with ToolCallInfo object directly.
   */
  @property({ type: Object })
  toolCall?: ToolCallInfo;

  override render() {
    const status = this.toolCall?.status ?? this.status;
    const name = this.toolCall?.name ?? (this.args.name as string) ?? "Tool";

    const statusIcon = {
      pending: "icon-hourglass",
      running: "icon-sync",
      completed: "icon-check",
      error: "icon-alert",
    }[status];

    return html`
      <div class="tool-status ${status}">
        <uui-icon name=${statusIcon}></uui-icon>
        <span class="tool-name">${name}</span>
        ${status === "running"
          ? html`<uui-loader-circle></uui-loader-circle>`
          : ""}
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

    .tool-status.completed {
      color: var(--uui-color-positive);
    }

    .tool-status.error {
      color: var(--uui-color-danger);
    }

    .tool-status.running uui-icon {
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
