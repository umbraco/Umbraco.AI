import { customElement, property, state, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestUaiAgentTool } from "../../tools/index.js";
import type { UaiInterruptInfo } from "../../types.js";
import type { UaiApprovalBaseConfig } from "./approval-base.element.js";

/**
 * HITL (Human-in-the-Loop) approval element for interrupt handling.
 *
 * This element handles the HITL-specific logic:
 * - Receives UaiInterruptInfo from AG-UI or frontend tool executor
 * - Maps interrupt type/tool to appropriate approval element config
 * - Serializes typed response to JSON for transport
 * - Dispatches 'respond' event with JSON string for HITL context
 *
 * Uses uai-approval-base internally for the actual UI rendering.
 *
 * @element uai-copilot-hitl-approval
 * @fires respond - Dispatched with JSON-serialized response string
 */
@customElement("uai-copilot-hitl-approval")
export class UaiCopilotHitlApprovalElement extends UmbLitElement {
  @property({ type: Object })
  interrupt?: UaiInterruptInfo;

  @state()
  private _baseConfig?: UaiApprovalBaseConfig;

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("interrupt") && this.interrupt) {
      this.#buildBaseConfig();
    }
  }

  /**
   * Build the base config from the interrupt info.
   * Maps interrupt type and tool manifest to approval element configuration.
   */
  #buildBaseConfig() {
    if (!this.interrupt) return;

    let elementAlias = "Uai.AgentApprovalElement.Default";
    let config: Record<string, unknown> = {};
    const args: Record<string, unknown> = this.interrupt.payload ?? {
      title: this.interrupt.title,
      message: this.interrupt.message,
      options: this.interrupt.options,
    };

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
        elementAlias = isSimple
          ? "Uai.AgentApprovalElement.Default"
          : (approvalObj?.elementAlias ?? "Uai.AgentApprovalElement.Default");
        config = isSimple ? {} : (approvalObj?.config ?? {});
      }
      
    } else {
      
      elementAlias = "Uai.AgentApprovalElement.Default";
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
      
    }

    this._baseConfig = {
      elementAlias,
      config,
      args,
    };
  }

  /**
   * Handle typed response from base element.
   * Serializes to JSON and dispatches event for HITL context.
   */
  #handleResponse = (response: unknown) => {
    // Serialize typed response to JSON for transport
    const jsonResponse = JSON.stringify(response);

    this.dispatchEvent(
      new CustomEvent("respond", {
        detail: jsonResponse,
        bubbles: true,
        composed: true,
      })
    );
  };

  override render() {
    if (!this.interrupt) {
      return nothing;
    }

    if (!this._baseConfig) {
      return html`
        <div class="interrupt-card">
          <div class="loading">
            <uui-loader></uui-loader>
          </div>
        </div>
      `;
    }

    return html`
      <div class="interrupt-card">
        <uai-approval-base
          .config=${this._baseConfig}
          .onResponse=${this.#handleResponse}
        ></uai-approval-base>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      margin-top: var(--uui-size-space-2);
    }

    .interrupt-card {
      display: inline-block;
      margin: 0 var(--uui-size-space-3);
      padding: var(--uui-size-space-3);
      background: var(--uui-color-surface-alt);
      border-radius: var(--uui-border-radius);
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: var(--uui-size-space-4);
    }
  `;
}

export default UaiCopilotHitlApprovalElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-copilot-hitl-approval": UaiCopilotHitlApprovalElement;
  }
}
