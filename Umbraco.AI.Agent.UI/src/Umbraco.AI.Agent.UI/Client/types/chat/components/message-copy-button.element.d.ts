import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
/**
 * Copy button component for chat messages.
 * Copies the provided content to clipboard and shows visual feedback.
 */
export declare class UaiMessageCopyButtonElement extends UmbLitElement {
    #private;
    content: string;
    private _copied;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiMessageCopyButtonElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-message-copy-button": UaiMessageCopyButtonElement;
    }
}
//# sourceMappingURL=message-copy-button.element.d.ts.map