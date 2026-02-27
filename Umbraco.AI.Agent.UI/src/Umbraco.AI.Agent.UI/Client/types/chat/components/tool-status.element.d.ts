import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolElementProps, UaiAgentToolStatus } from "../types/tool.types.js";
/**
 * Default element for displaying tool call status.
 * Shows an icon, tool name, and loading indicator based on status.
 */
export declare class UaiAgentToolStatusElement extends UmbLitElement implements UaiAgentToolElementProps {
    args: Record<string, unknown>;
    status: UaiAgentToolStatus;
    result?: unknown;
    /** Display name for the tool */
    name: string;
    /** Icon to display */
    icon: string;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiAgentToolStatusElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-tool-status": UaiAgentToolStatusElement;
    }
}
//# sourceMappingURL=tool-status.element.d.ts.map