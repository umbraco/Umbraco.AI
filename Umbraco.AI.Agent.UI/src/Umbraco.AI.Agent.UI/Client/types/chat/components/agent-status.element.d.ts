import { nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentState } from "../types/index.js";
/**
 * Agent status component.
 * Shows agent thinking/progress state.
 */
export declare class UaiAgentStatusElement extends UmbLitElement {
    #private;
    state?: UaiAgentState;
    render(): import("lit-html").TemplateResult<1> | typeof nothing;
    static styles: import("lit").CSSResult;
}
export default UaiAgentStatusElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-status": UaiAgentStatusElement;
    }
}
//# sourceMappingURL=agent-status.element.d.ts.map