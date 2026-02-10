import { nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiInterruptInfo } from "../types/index.js";
/**
 * HITL (Human-in-the-Loop) approval element for interrupt handling.
 *
 * Uses uai-approval-base internally for the actual UI rendering.
 *
 * @element uai-hitl-approval
 * @fires respond - Dispatched with JSON-serialized response string
 */
export declare class UaiHitlApprovalElement extends UmbLitElement {
    #private;
    interrupt?: UaiInterruptInfo;
    private _baseConfig?;
    updated(changedProperties: Map<string, unknown>): void;
    render(): import("lit-html").TemplateResult<1> | typeof nothing;
    static styles: import("lit").CSSResult;
}
export default UaiHitlApprovalElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-hitl-approval": UaiHitlApprovalElement;
    }
}
//# sourceMappingURL=hitl-approval.element.d.ts.map