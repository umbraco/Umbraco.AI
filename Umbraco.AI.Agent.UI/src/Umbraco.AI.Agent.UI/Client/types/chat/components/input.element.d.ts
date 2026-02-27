import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
/**
 * Chat input component.
 * Provides a text input with send button, agent selector, and keyboard support.
 * Consumes UAI_CHAT_CONTEXT for agent data.
 *
 * @fires send - Dispatched when user sends a message
 */
export declare class UaiChatInputElement extends UmbLitElement {
    #private;
    disabled: boolean;
    placeholder: string;
    private _value;
    private _agents;
    private _selectedAgentId;
    constructor();
    updated(changedProperties: PropertyValues): void;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiChatInputElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-input": UaiChatInputElement;
    }
}
//# sourceMappingURL=input.element.d.ts.map