import { customElement, property, state, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { ManifestUaiAgentTool } from "../../../../agent/tools/uai-agent-tool.extension.js";
import type {
  ManifestUaiAgentApprovalElement,
  UaiAgentApprovalElement,
} from "../../../../agent/approval/uai-agent-approval-element.extension.js";
import type { UaiInterruptInfo } from "../../../core/types.js";

/**
 * Interrupt UI component.
 * Renders approval/input/choice UI for human-in-the-loop interactions.
 *
 * Uses the uaiAgentApprovalElement extension system when available.
 * Falls back to built-in approval elements based on interrupt type.
 *
 * @fires respond - Dispatched when user responds to the interrupt
 */
@customElement("uai-copilot-interrupt")
export class UaiCopilotInterruptElement extends UmbLitElement {
  #approvalElement: UaiAgentApprovalElement | null = null;

  @property({ type: Object })
  interrupt?: UaiInterruptInfo;

  @state()
  private _isLoading = true;

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("interrupt") && this.interrupt) {
      this.#loadApprovalElement();
    }
  }

  /**
   * Load the appropriate approval element based on the interrupt.
   * Priority:
   * 1. Tool manifest with approval config (if toolName in payload)
   * 2. Default approval element based on interrupt type
   */
  async #loadApprovalElement() {
    if (!this.interrupt) return;

    this._isLoading = true;
    this.#approvalElement = null;

    // Determine which approval element to use
    let alias = "Uai.AgentApprovalElement.Default";
    let config: Record<string, unknown> = {};

    // Check if interrupt payload has a toolName - if so, look up tool manifest
    const toolName = this.interrupt.payload?.toolName as string | undefined;

    if (toolName) {
      // Try to find a UaiAgentTool manifest for this tool
      const toolManifests = umbExtensionsRegistry.getByTypeAndFilter<
        "uaiAgentTool",
        ManifestUaiAgentTool
      >("uaiAgentTool", (m) => m.meta.toolName === toolName);

      if (toolManifests.length > 0 && toolManifests[0].meta.approval) {
        const approval = toolManifests[0].meta.approval;
        const isSimple = approval === true;
        const approvalObj = typeof approval === "object" ? approval : null;
        alias = isSimple
          ? "Uai.AgentApprovalElement.Default"
          : (approvalObj?.elementAlias ?? "Uai.AgentApprovalElement.Default");
        config = isSimple
          ? {}
          : (approvalObj?.config ?? {});
      }
    } else {
      // Map interrupt type to default approval element
      switch (this.interrupt.type) {
        case "input":
          alias = "Uai.AgentApprovalElement.Input";
          config = {
            prompt: this.interrupt.title,
            placeholder: this.interrupt.inputConfig?.placeholder,
            multiline: this.interrupt.inputConfig?.multiline,
          };
          break;
        case "choice":
          alias = "Uai.AgentApprovalElement.Choice";
          config = {
            title: this.interrupt.title,
            message: this.interrupt.message,
            options: this.interrupt.options?.map((opt) => ({
              value: opt.value,
              label: opt.label,
              variant: opt.variant,
            })),
          };
          break;
        case "approval":
        default:
          alias = "Uai.AgentApprovalElement.Default";
          config = {
            title: this.interrupt.title,
            message: this.interrupt.message,
          };
          // Map options to approve/deny labels if provided
          if (this.interrupt.options?.length) {
            const approveOpt = this.interrupt.options.find(
              (o) => o.variant === "positive" || o.value === "yes"
            );
            const denyOpt = this.interrupt.options.find(
              (o) => o.variant === "danger" || o.value === "no"
            );
            if (approveOpt) config.approveLabel = approveOpt.label;
            if (denyOpt) config.denyLabel = denyOpt.label;
          }
          break;
      }
    }

    // Look up approval element manifest by alias
    const approvalManifests = umbExtensionsRegistry.getByTypeAndFilter<
      "uaiAgentApprovalElement",
      ManifestUaiAgentApprovalElement
    >("uaiAgentApprovalElement", (manifest) => manifest.alias === alias);

    if (approvalManifests.length === 0) {
      console.error("Approval element not found: " + alias);
      this._isLoading = false;
      return;
    }

    const approvalManifest = approvalManifests[0];

    if (!approvalManifest.element) {
      console.error("Approval element has no element: " + alias);
      this._isLoading = false;
      return;
    }

    try {
      const ElementConstructor = await loadManifestElement<UaiAgentApprovalElement>(approvalManifest.element);

      if (ElementConstructor) {
        this.#approvalElement = new ElementConstructor();

        // Build args from interrupt payload or legacy fields
        const args: Record<string, unknown> = this.interrupt.payload ?? {
          title: this.interrupt.title,
          message: this.interrupt.message,
          options: this.interrupt.options,
        };

        this.#approvalElement.args = args;
        this.#approvalElement.config = config;
        this.#approvalElement.respond = (result: unknown) => this.#handleResponse(result);
      }
    } catch (error) {
      console.error("Failed to load approval element " + alias + ":", error);
    }

    this._isLoading = false;
    this.requestUpdate();
  }

  #handleResponse(value: unknown) {
    // Convert response to string for backward compatibility with existing event handlers
    let responseString: string;

    if (typeof value === "string") {
      responseString = value;
    } else if (typeof value === "object" && value !== null) {
      // Handle standard approval response formats
      const obj = value as Record<string, unknown>;
      if (obj.approved === true) {
        responseString = "yes";
      } else if (obj.approved === false) {
        responseString = "no";
      } else if (obj.input !== undefined) {
        responseString = String(obj.input);
      } else if (obj.choice !== undefined) {
        responseString = String(obj.choice);
      } else if (obj.cancelled === true) {
        responseString = "__cancelled__";
      } else {
        responseString = JSON.stringify(value);
      }
    } else {
      responseString = String(value);
    }

    this.dispatchEvent(
      new CustomEvent("respond", {
        detail: responseString,
        bubbles: true,
        composed: true,
      })
    );
  }

  override render() {
    if (!this.interrupt) {
      return nothing;
    }

    if (this._isLoading) {
      return html`
        <div class="interrupt-card">
          <div class="loading">
            <uui-loader></uui-loader>
          </div>
        </div>
      `;
    }

    if (this.#approvalElement) {
      return html`
        <div class="interrupt-card">
          ${this.#approvalElement}
        </div>
      `;
    }

    // Fallback if no approval element was loaded
    return html`
      <div class="interrupt-card">
        <div class="interrupt-header">
          <uui-icon name="icon-alert"></uui-icon>
          <span>${this.interrupt.title}</span>
        </div>
        <div class="interrupt-message">${this.interrupt.message}</div>
        <div class="interrupt-actions">
          <uui-button look="primary" color="positive" @click=${() => this.#handleResponse({ approved: true })}>
            Approve
          </uui-button>
          <uui-button look="secondary" @click=${() => this.#handleResponse({ approved: false })}>
            Deny
          </uui-button>
        </div>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .interrupt-card {
      margin: var(--uui-size-space-3);
      padding: var(--uui-size-space-4);
      background: var(--uui-color-surface-alt);
      border: 1px solid var(--uui-color-warning);
      border-radius: var(--uui-border-radius);
    }

    .interrupt-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      font-weight: 600;
      color: var(--uui-color-warning-standalone);
      margin-bottom: var(--uui-size-space-3);
    }

    .interrupt-message {
      margin-bottom: var(--uui-size-space-4);
      line-height: 1.5;
    }

    .interrupt-actions {
      display: flex;
      gap: var(--uui-size-space-2);
      flex-wrap: wrap;
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: var(--uui-size-space-4);
    }
  `;
}

export default UaiCopilotInterruptElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-interrupt": UaiCopilotInterruptElement;
  }
}
