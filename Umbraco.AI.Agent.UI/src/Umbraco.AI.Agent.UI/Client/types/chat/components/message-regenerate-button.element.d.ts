import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
/**
 * Regenerate button component for chat messages.
 *
 * @fires regenerate - Dispatched when the user clicks to regenerate the response
 */
export declare class UaiMessageRegenerateButtonElement extends UmbLitElement {
    #private;
    render(): import("lit-html").TemplateResult<1>;
    static styles: import("lit").CSSResult;
}
export default UaiMessageRegenerateButtonElement;
declare global {
    interface HTMLElementTagNameMap {
        "uai-message-regenerate-button": UaiMessageRegenerateButtonElement;
    }
}
//# sourceMappingURL=message-regenerate-button.element.d.ts.map