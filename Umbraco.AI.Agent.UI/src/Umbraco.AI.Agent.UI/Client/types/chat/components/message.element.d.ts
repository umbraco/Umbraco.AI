import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiChatMessage } from "../types/index.js";
/**
 * Chat message component.
 * Renders a single message with markdown support and embedded tool status.
 */
export declare class UaiChatMessageElement extends UmbLitElement {
    #private;
    message: UaiChatMessage;
    isLastAssistantMessage: boolean;
    isRunning: boolean;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiChatMessageElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-message": UaiChatMessageElement;
    }
}
//# sourceMappingURL=message.element.d.ts.map