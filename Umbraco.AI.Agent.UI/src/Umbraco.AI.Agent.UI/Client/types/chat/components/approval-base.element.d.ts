import { nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentApprovalElement } from "../extensions/uai-agent-approval-element.extension.js";
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
 * @element uai-approval-base
 * @fires response - Dispatched with typed response when user responds
 */
export declare class UaiApprovalBaseElement extends UmbLitElement {
    #private;
    config: UaiApprovalBaseConfig;
    onResponse?: (response: unknown) => void;
    private _isLoading;
    private _error?;
    updated(changedProperties: Map<string, unknown>): void;
    render(): import("lit-html").TemplateResult<1> | typeof nothing | UaiAgentApprovalElement;
    static styles: import("lit").CSSResult;
}
export default UaiApprovalBaseElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-approval-base": UaiApprovalBaseElement;
    }
}
//# sourceMappingURL=approval-base.element.d.ts.map