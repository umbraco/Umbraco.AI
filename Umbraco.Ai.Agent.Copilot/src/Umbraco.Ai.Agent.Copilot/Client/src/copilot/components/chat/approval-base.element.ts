import { customElement, property, state, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { loadManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type {
  ManifestUaiAgentApprovalElement,
  UaiAgentApprovalElement,
} from "../../approval/index.js";

/**
 * Configuration for the base approval element.
 */
export interface UaiApprovalBaseConfig {
  /** Alias of the approval element to load (defaults to Uai.AgentApprovalElement.Default) */
  elementAlias?: string;
  /** Static config to pass to the approval element */
  config?: Record<string, unknown>;
  /** Arguments to pass to the approval element (typically from LLM) */
  args?: Record<string, unknown>;
}

/**
 * Base approval element that loads and renders approval UI components.
 *
 * This is a pure UI component that:
 * - Loads approval elements from the extension registry by alias
 * - Passes config and args to the loaded element
 * - Returns TYPED responses via the onResponse callback
 *
 * This element has no knowledge of HITL protocols or interrupts.
 * Consumers (HITL wrapper, tool executor) handle response serialization as needed.
 *
 * @element uai-approval-base
 * @fires response - Dispatched with typed response when user responds
 */
@customElement("uai-approval-base")
export class UaiApprovalBaseElement extends UmbLitElement {
  #approvalElement: UaiAgentApprovalElement | null = null;

  @property({ type: Object })
  config: UaiApprovalBaseConfig = {};

  /**
   * Callback for typed response. Set this to receive the response directly.
   * Alternative to listening for the 'response' event.
   */
  @property({ attribute: false })
  onResponse?: (response: unknown) => void;

  @state()
  private _isLoading = true;

  @state()
  private _error?: string;

  override updated(changedProperties: Map<string, unknown>) {
    super.updated(changedProperties);

    if (changedProperties.has("config") && this.config) {
      this.#loadApprovalElement();
    }
  }

  /**
   * Load the appropriate approval element based on config.
   */
  async #loadApprovalElement() {
    this._isLoading = true;
    this._error = undefined;
    this.#approvalElement = null;

    const alias = this.config.elementAlias ?? "Uai.AgentApprovalElement.Default";

    // Look up approval element manifest by alias
    const approvalManifests = umbExtensionsRegistry.getByTypeAndFilter<
      "uaiAgentApprovalElement",
      ManifestUaiAgentApprovalElement
    >("uaiAgentApprovalElement", (manifest) => manifest.alias === alias);

    if (approvalManifests.length === 0) {
      this._error = `Approval element not found: ${alias}`;
      console.error(this._error);
      this._isLoading = false;
      return;
    }

    const approvalManifest = approvalManifests[0];

    if (!approvalManifest.element) {
      this._error = `Approval element has no element defined: ${alias}`;
      console.error(this._error);
      this._isLoading = false;
      return;
    }

    try {
      const ElementConstructor = await loadManifestElement<UaiAgentApprovalElement>(
        approvalManifest.element
      );

      if (ElementConstructor) {
        this.#approvalElement = new ElementConstructor();
        this.#approvalElement.args = this.config.args ?? {};
        this.#approvalElement.config = this.config.config ?? {};
        this.#approvalElement.respond = (result: unknown) => this.#handleResponse(result);
      }
    } catch (error) {
      this._error = `Failed to load approval element ${alias}: ${error}`;
      console.error(this._error);
    }

    this._isLoading = false;
    this.requestUpdate();
  }

  /**
   * Handle response from the approval element.
   * Emits typed response via callback and event.
   */
  #handleResponse(response: unknown) {
    // Call callback if provided
    if (this.onResponse) {
      this.onResponse(response);
    }

    // Also dispatch event with typed response
    this.dispatchEvent(
      new CustomEvent("response", {
        detail: response,
        bubbles: true,
        composed: true,
      })
    );
  }

  override render() {
    if (this._isLoading) {
      return html`
        <div class="loading">
          <uui-loader></uui-loader>
        </div>
      `;
    }

    if (this._error) {
      return html`
        <div class="error">
          <uui-icon name="icon-alert"></uui-icon>
          <span>${this._error}</span>
        </div>
      `;
    }

    if (this.#approvalElement) {
      return this.#approvalElement;
    }

    return nothing;
  }

  static override styles = css`
    :host {
      display: block;
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: var(--uui-size-space-4);
    }

    .error {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-space-4);
      color: var(--uui-color-danger);
    }
  `;
}

export default UaiApprovalBaseElement;

declare global {
  interface HTMLElementTagNameMap {
    "uai-approval-base": UaiApprovalBaseElement;
  }
}
