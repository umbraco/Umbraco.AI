import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_VALIDATION_EMPTY_LOCALIZATION_KEY, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";

const elementName = "uai-property-editor-ui-masked-text-box";

/**
 * Determines whether a value is a configuration reference rather than a literal secret.
 *
 * Mirrors the server-side rule in `AIEditableModelResolver`: a single leading `$` denotes a
 * configuration key, while `$$` is the escape hatch for a literal value that happens to start
 * with `$` — and a literal is still a secret, so it stays masked.
 */
function isConfigReference(value: unknown): boolean {
    return typeof value === "string" && value.startsWith("$") && !value.startsWith("$$");
}

/**
 * Text box that masks its value by default, with a built-in toggle to reveal it.
 *
 * Applied automatically to fields marked `[AIField(IsSensitive = true)]` — the schema builder
 * infers this alias server-side, so `isSensitive` never needs to reach the client. It stops
 * credentials sitting in plain sight during screen shares and demos. Note the value still
 * travels to the browser in full, so this guards against being read over someone's shoulder,
 * not against anyone who opens dev tools.
 *
 * Configuration references (`$Umbraco:AI:Secrets:ApiKey`) start revealed: they are pointers,
 * not secrets, and hiding them makes it impossible to see which key a connection points at.
 *
 * This drives `uui-input-password`'s type rather than swapping in a different element, so the
 * masking keeps up with what is actually in the field as it is typed. Replacing the element
 * would be the obvious way to drop the reveal toggle for a reference, but it destroys and
 * recreates the input — which takes the caret with it mid-edit.
 */
@customElement(elementName)
export class UaiPropertyEditorUIMaskedTextBoxElement
    extends UmbFormControlMixin<string, typeof UmbLitElement, undefined>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean, reflect: true })
    readonly = false;

    @property({ type: Boolean })
    mandatory?: boolean;

    @property({ type: String })
    mandatoryMessage = UMB_VALIDATION_EMPTY_LOCALIZATION_KEY;

    /**
     * The name of this field.
     */
    @property({ type: String })
    name?: string;

    @state()
    private _placeholder?: string;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;

        this._placeholder = this.localize.string(config.getValueByAlias<string>("placeholder") ?? "");
    }

    protected override firstUpdated(): void {
        this.addFormControlElement(this.shadowRoot!.querySelector("uui-input-password")!);
    }

    override focus() {
        return this.shadowRoot?.querySelector<UUIInputElement>("uui-input-password")?.focus();
    }

    #onInput(e: InputEvent) {
        const newValue = (e.target as HTMLInputElement).value;
        if (newValue === this.value) return;

        this.value = newValue;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        // Only re-committed when the bound value itself changes, so a reveal the user toggled by
        // hand survives further typing — Lit skips the property write while this stays the same.
        const type = isConfigReference(this.value) ? "text" : "password";

        return html`
            <uui-input-password
                .label=${this.localize.term("general_fieldFor", [this.name])}
                .placeholder=${this._placeholder ?? ""}
                .requiredMessage=${this.mandatoryMessage}
                .type=${type}
                .value=${this.value ?? ""}
                ?readonly=${this.readonly}
                ?required=${this.mandatory}
                autocomplete="off"
                @input=${this.#onInput}
            >
            </uui-input-password>
        `;
    }

    static override styles = [
        css`
            uui-input-password {
                width: 100%;
            }
        `,
    ];
}

export { UaiPropertyEditorUIMaskedTextBoxElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIMaskedTextBoxElement;
    }
}
