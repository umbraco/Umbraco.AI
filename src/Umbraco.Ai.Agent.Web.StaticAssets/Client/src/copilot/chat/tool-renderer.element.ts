import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestElement, loadManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { ManifestUaiAgentTool, UaiAgentToolElement, UaiAgentToolStatus, UaiAgentToolApi } from "../../agent/tools/uai-agent-tool.extension.js";
import type { ToolCallInfo, ToolCallStatus } from "./types.js";

// Import default tool status component
import "../../agent/tools/tool-status.element.js";

/**
 * Event detail for tool execution results.
 */
export interface ToolResultEventDetail {
  toolCallId: string;
  result: unknown;
  error?: string;
}

/**
 * Tool renderer component that dynamically renders tool UI based on registered extensions.
 *
 * For each tool call:
 * 1. Looks up `uaiAgentTool` extension by `meta.toolName`
 * 2. If found with `element`, instantiates the custom element
 * 3. Otherwise, renders the default tool status indicator
 *
 * Supports both backend tools (render-only) and frontend tools (with execution).
 */
@customElement("uai-tool-renderer")
export class UaiToolRendererElement extends UmbLitElement {
  #toolElement: UaiAgentToolElement | null = null;
  #manifest: ManifestUaiAgentTool | null = null;
  #toolApi: UaiAgentToolApi | null = null;
  #hasExecuted = false;
  #previousToolCallStatus: ToolCallStatus | null = null;

  @property({ type: Object })
  toolCall!: ToolCallInfo;

  @state()
  private _status: UaiAgentToolStatus = "pending";

  @state()
  private _result?: unknown;

  @state()
  private _hasCustomElement = false;

  override connectedCallback() {
    super.connectedCallback();
    this.#lookupExtension();
  }

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("toolCall") && this.toolCall) {
      // Check if status just changed to a state where we should execute
      const statusChanged = this.#previousToolCallStatus !== this.toolCall.status;
      this.#previousToolCallStatus = this.toolCall.status;

      // Update status based on tool call info
      this.#updateStatus();

      // Update element props if we have a custom element
      if (this.#toolElement) {
        this.#updateElementProps();
      }

      // Execute frontend tool when args are complete (status changes from streaming/pending)
      // and we have an API to execute
      if (
        statusChanged &&
        !this.#hasExecuted &&
        this.hasFrontendApi &&
        this.toolCall.arguments &&
        this.toolCall.status !== "streaming"
      ) {
        this.#executeFrontendTool();
      }
    }
  }

  /**
   * Look up the tool extension by toolName.
   */
  #lookupExtension() {
    if (!this.toolCall?.name) return;

    const manifests = umbExtensionsRegistry.getByTypeAndFilter<
      "uaiAgentTool",
      ManifestUaiAgentTool
    >("uaiAgentTool", (manifest) => manifest.meta.toolName === this.toolCall.name);

    if (manifests.length > 0) {
      this.#manifest = manifests[0];
      this.#loadElement();
    }
  }

  /**
   * Load the custom element and API if the manifest has them.
   */
  async #loadElement() {
    if (!this.#manifest) return;

    // Load custom element if available
    if (this.#manifest.element) {
      try {
        const ElementConstructor = await loadManifestElement<UaiAgentToolElement>(this.#manifest.element);

        if (ElementConstructor) {
          this.#toolElement = new ElementConstructor();
          this.#updateElementProps();
          this._hasCustomElement = true;
        }
      } catch (error) {
        console.error(`Failed to load tool element for ${this.toolCall.name}:`, error);
      }
    }

    // Load API if available (for frontend tools)
    if (this.#manifest.api) {
      try {
        const ApiConstructor = await loadManifestApi<UaiAgentToolApi>(this.#manifest.api);

        if (ApiConstructor) {
          this.#toolApi = new ApiConstructor(this);
        }
      } catch (error) {
        console.error(`Failed to load tool API for ${this.toolCall.name}:`, error);
      }
    }
  }

  /**
   * Execute a frontend tool and dispatch the result.
   */
  async #executeFrontendTool() {
    if (!this.#toolApi || this.#hasExecuted) return;

    this.#hasExecuted = true;
    this._status = "executing";

    // Parse arguments
    let args: Record<string, unknown> = {};
    if (this.toolCall.arguments) {
      try {
        args = JSON.parse(this.toolCall.arguments);
      } catch {
        args = { raw: this.toolCall.arguments };
      }
    }

    try {
      const result = await this.#toolApi.execute(args);

      this._result = result;
      this._status = "complete";

      // Update element props
      if (this.#toolElement) {
        this.#updateElementProps();
      }

      // Dispatch event with result
      this.dispatchEvent(
        new CustomEvent<ToolResultEventDetail>("tool-result", {
          detail: {
            toolCallId: this.toolCall.id,
            result,
          },
          bubbles: true,
          composed: true,
        })
      );
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this._status = "error";
      this._result = { error: errorMessage };

      // Update element props
      if (this.#toolElement) {
        this.#updateElementProps();
      }

      // Dispatch event with error
      this.dispatchEvent(
        new CustomEvent<ToolResultEventDetail>("tool-result", {
          detail: {
            toolCallId: this.toolCall.id,
            result: { error: errorMessage },
            error: errorMessage,
          },
          bubbles: true,
          composed: true,
        })
      );
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
      executing: "executing",
      completed: "complete",
      error: "error",
    };

    this._status = statusMap[this.toolCall.status] ?? "pending";

    if (this.toolCall.result) {
      try {
        this._result = JSON.parse(this.toolCall.result);
      } catch {
        this._result = this.toolCall.result;
      }
    }
  }

  /**
   * Update the custom element's props.
   */
  #updateElementProps() {
    if (!this.#toolElement) return;

    // Parse arguments
    let args: Record<string, unknown> = {};
    if (this.toolCall.arguments) {
      try {
        args = JSON.parse(this.toolCall.arguments);
      } catch {
        args = { raw: this.toolCall.arguments };
      }
    }

    // Set props on the element
    this.#toolElement.args = args;
    this.#toolElement.status = this._status;
    this.#toolElement.result = this._result;
  }

  /**
   * Get the manifest for external use (e.g., for frontend tool execution).
   */
  get manifest(): ManifestUaiAgentTool | null {
    return this.#manifest;
  }

  /**
   * Check if this tool has an API (is a frontend tool).
   */
  get hasFrontendApi(): boolean {
    return this.#manifest?.api !== undefined || this.#toolApi !== null;
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
    let args: Record<string, unknown> = {};
    if (this.toolCall?.arguments) {
      try {
        args = JSON.parse(this.toolCall.arguments);
      } catch {
        args = { raw: this.toolCall.arguments };
      }
    }

    return html`
      <uai-agent-tool-status
        .name=${this.#manifest?.meta.label ?? this.toolCall?.name ?? "Tool"}
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
