import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import type { ManifestUaiAgentToolRenderer } from "../extensions/uai-agent-tool-renderer.extension.js";
import type { UaiAgentToolElement, UaiAgentToolStatus } from "../types/tool.types.js";
import type { UaiToolCallInfo } from "../types/index.js";
import { safeParseJson } from "../utils/json.js";
import { UAI_CHAT_CONTEXT } from "../context.js";
import type { UaiToolRendererManager } from "../services/tool-renderer.manager.js";

/**
 * Tool renderer component that dynamically renders tool UI based on registered extensions.
 *
 * This is a purely presentational component that:
 * 1. Looks up `uaiAgentToolRenderer` extension by `meta.toolName`
 * 2. If found with `element`, instantiates the custom element
 * 3. Otherwise, renders the default tool status indicator
 *
 * Consumes UAI_CHAT_CONTEXT for the tool renderer manager.
 */
@customElement("uai-tool-renderer")
export class UaiToolRendererElement extends UmbLitElement {
    #toolRendererManager?: UaiToolRendererManager;
    #toolElement: UaiAgentToolElement | null = null;
    #manifest: ManifestUaiAgentToolRenderer | null = null;

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
        this.consumeContext(UAI_CHAT_CONTEXT, (context) => {
            if (context) {
                this.#toolRendererManager = context.toolRendererManager;
                this.#lookupExtension();
            }
        });
    }

    override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);

        if (changedProperties.has("toolCall") && this.toolCall) {
            this.#updateStatus();

            if (this.#toolElement) {
                this.#updateElementProps();
            }
        }
    }

    #lookupExtension() {
        if (!this.#toolRendererManager || !this.toolCall?.name || this.#manifest) {
            return;
        }
        this.#manifest = this.#toolRendererManager.getManifest(this.toolCall.name) ?? null;
        if (this.#manifest) {
            this.#loadElement();
        }
    }

    async #loadElement() {
        if (!this.#toolRendererManager || !this.toolCall?.name) return;

        try {
            const ElementConstructor = await this.#toolRendererManager.getElement(this.toolCall.name);

            if (ElementConstructor) {
                const existing = this.renderRoot.firstElementChild;
                if (existing instanceof ElementConstructor) {
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

    #updateStatus() {
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
                this.toolCall.result as unknown as Record<string, unknown>,
            );
        }
    }

    #updateElementProps() {
        if (!this.#toolElement) return;

        const args = safeParseJson(this.toolCall.arguments, { raw: this.toolCall.arguments });

        this.#toolElement.args = args;
        this.#toolElement.status = this._status;
        this.#toolElement.result = this._result;
    }

    get manifest(): ManifestUaiAgentToolRenderer | null {
        return this.#manifest;
    }

    override render() {
        if (this._hasCustomElement && this.#toolElement) {
            return html`${this.#toolElement}`;
        }

        return this.#renderDefault();
    }

    #renderDefault() {
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
