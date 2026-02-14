import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentApprovalElement } from "../../extensions/uai-agent-approval-element.extension.js";
/**
 * Default approval element with Approve/Deny buttons.
 *
 * Displays a confirmation dialog with customizable buttons.
 * Priority order for display values: config -> args -> localized defaults
 *
 * @element uai-agent-approval-default
 */
export declare class UaiAgentApprovalDefaultElement extends UmbLitElement implements UaiAgentApprovalElement {
    #private;
    args: Record<string, unknown>;
    config: Record<string, unknown>;
    respond: (result: unknown) => void;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiAgentApprovalDefaultElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-approval-default": UaiAgentApprovalDefaultElement;
    }
}
//# sourceMappingURL=default.element.d.ts.map