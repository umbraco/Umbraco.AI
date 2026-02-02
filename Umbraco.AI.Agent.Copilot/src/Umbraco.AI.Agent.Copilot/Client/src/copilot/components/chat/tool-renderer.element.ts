import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import type {
  ManifestUaiAgentTool,
  UaiAgentToolElement,
  UaiAgentToolStatus,
} from "../../tools";
import type { UaiToolCallInfo } from "../../types.js";
import { safeParseJson } from "../../utils";
import { UAI_COPILOT_CONTEXT } from "../../copilot.context.js";
import type { UaiToolManager } from "../../services/tool.manager.ts";

/**
 * Tool renderer component that dynamically renders tool UI based on registered extensions.
 *
 * This is a purely presentational component that:
 * 1. Looks up `uaiAgentTool` extension by `meta.toolName`
 * 2. If found with `element`, instantiates the custom element
 * 3. Otherwise, renders the default tool status indicator
 *
 * Status updates and results are received via the `toolCall` property,
 * which is updated by the controller when the tool bus emits events.
 *
 * Execution is handled by UaiFrontendToolExecutor, NOT by this component.
 */
@customElement("uai-tool-renderer")
export class UaiToolRendererElement extends UmbLitElement {
  #toolManager?: UaiToolManager;
  #toolElement: UaiAgentToolElement | null = null;
  #manifest: ManifestUaiAgentTool | null = null;

  @property({ type: Object })
  toolCall!: UaiToolCallInfo;

  @state()
  private _status: UaiAgentToolStatus = "pending";

  @state()
  private _result?: unknown;

  @state()
  private _hasCustomElement = false;

  override connectedCallback() {
    super.connectedCallback();
    this.consumeContext(UAI_COPILOT_CONTEXT, (context) => {
      if (context) {
        this.#toolManager = context.toolManager;
        this.#lookupExtension();
      }
    });
  }

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("toolCall") && this.toolCall) {
      // Update status based on tool call info
      this.#updateStatus();

      // Update element props if we have a custom element
      if (this.#toolElement) {
        this.#updateElementProps();
      }
    }
  }

  /**
   * Look up the tool extension by toolName.
   */
  #lookupExtension() {
    if (!this.#toolManager || !this.toolCall?.name || this.#manifest) {
      return;
    }
    this.#manifest = this.#toolManager.getManifest(this.toolCall.name) ?? null;
    if (this.#manifest) {
      this.#loadElement();
    }
  }

  /**
   * Load the custom element if the manifest has one.
   * Reuses existing element from the template if it matches the expected type.
   */
  async #loadElement() {
    if (!this.#toolManager || !this.toolCall?.name) return;

    try {
      const ElementConstructor = await this.#toolManager.getElement(this.toolCall.name);

      if (ElementConstructor) {
        // Check if we already rendered an element of this type via the template
        const existing = this.renderRoot.firstElementChild;
        if (existing instanceof ElementConstructor) {
          // Reuse the existing element instead of creating a new one
          this.#toolElement = existing as UaiAgentToolElement;
        } else {
          this.#toolElement = new ElementConstructor();
        }
        this.#updateElementProps();
        this._hasCustomElement = true;
      }
    } catch (error) {
      console.error(`Failed to load tool element for ${this.toolCall.name}:`, error);
    }
  }

  /**
   * Update the internal status based on toolCall info.
   */
  #updateStatus() {
    // Map the toolCall status to our internal status
    const statusMap: Record<string, UaiAgentToolStatus> = {
      pending: "pending",
      streaming: "streaming",
      awaiting_approval: "awaiting_approval",
      executing: "executing",
      completed: "complete",
      error: "error",
    };

    this._status = statusMap[this.toolCall.status] ?? "pending";

    if (this.toolCall.result) {
      this._result = safeParseJson(
        this.toolCall.result,
        this.toolCall.result as unknown as Record<string, unknown>
      );
    }
  }

  /**
   * Update the custom element's props.
   */
  #updateElementProps() {
    if (!this.#toolElement) return;

    // Parse arguments
    const args = safeParseJson(this.toolCall.arguments, { raw: this.toolCall.arguments });

    // Set standard props on the element
    this.#toolElement.args = args;
    this.#toolElement.status = this._status;
    this.#toolElement.result = this._result;
  }

  /**
   * Get the manifest for external use.
   */
  get manifest(): ManifestUaiAgentTool | null {
    return this.#manifest;
  }

  override render() {
    if (this._hasCustomElement && this.#toolElement) {
      return html`${this.#toolElement}`;
    }

    // Default rendering using tool-status component
    return this.#renderDefault();
  }

  #renderDefault() {
    // Parse arguments for display
    const args = safeParseJson(this.toolCall?.arguments, {
      raw: this.toolCall?.arguments ?? "",
    });

    return html`
      <uai-agent-tool-status
        .name=${this.toolCall?.name ?? "Tool"}
        .status=${this._status}
        .icon=${this.#manifest?.meta.icon ?? "icon-wand"}
        .args=${args}
        .result=${this._result}
      ></uai-agent-tool-status>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "uai-tool-renderer": UaiToolRendererElement;
  }
}
