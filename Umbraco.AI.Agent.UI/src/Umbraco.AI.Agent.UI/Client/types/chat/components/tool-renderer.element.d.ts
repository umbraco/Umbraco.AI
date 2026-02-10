import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { ManifestUaiAgentToolRenderer } from "../extensions/uai-agent-tool-renderer.extension.js";
import type { UaiToolCallInfo } from "../types/index.js";
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
export declare class UaiToolRendererElement extends UmbLitElement {
    #private;
    toolCall: UaiToolCallInfo;
    private _status;
    private _result?;
    private _hasCustomElement;
    connectedCallback(): void;
    updated(changedProperties: Map<string, unknown>): void;
    get manifest(): ManifestUaiAgentToolRenderer | null;
    render(): import("lit-html").TemplateResult<1>;
}
declare global {
    interface HTMLElementTagNameMap {
        "uai-tool-renderer": UaiToolRendererElement;
    }
}
//# sourceMappingURL=tool-renderer.element.d.ts.map