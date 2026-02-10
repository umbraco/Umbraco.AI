import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
/**
 * Main chat component.
 * Renders observables from the shared chat context and forwards user input.
 * Consumes UAI_CHAT_CONTEXT -- works in any surface (copilot, chat, etc.).
 */
export declare class UaiChatElement extends UmbLitElement {
    #private;
    private _agentName;
    private _messages;
    private _agentState?;
    private _pendingApproval?;
    private _isRunning;
    constructor();
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiChatElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-chat": UaiChatElement;
    }
}
//# sourceMappingURL=chat.element.d.ts.map