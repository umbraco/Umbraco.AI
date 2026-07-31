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
 * Configuration references (`$Umbraco:AI:Secrets:ApiKey`) render unmasked: they are pointers,
 * not secrets, and hiding them makes it impossible to see which key a connection points at.
 *
 * The masked/plain decision is re-evaluated only while the field is unfocused, so typing a
 * leading `$` doesn't swap the input out from under the cursor.
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
    private _isReference = false;

    @state()
    private _placeholder?: string;

    /**
     * The inner input currently registered for validation. Tracked because switching between the
     * masked and plain inputs replaces the element, and the stale one has to be unregistered.
     */
    #formControl?: UUIInputElement;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;

        this._placeholder = this.localize.string(config.getValueByAlias<string>("placeholder") ?? "");
    }

    override connectedCallback() {
        super.connectedCallback();
        this.addEventListener("focusout", this.#onFocusOut);
    }

    override disconnectedCallback() {
        this.removeEventListener("focusout", this.#onFocusOut);
        super.disconnectedCallback();
    }

    override willUpdate(changedProperties: Map<string, unknown>) {
        super.willUpdate(changedProperties);

        if (changedProperties.has("value")) {
            this.#syncReferenceState();
        }
    }

    override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);

        // Re-point validation at the inner input whenever the masked/plain swap replaces it.
        const input = this.shadowRoot?.querySelector<UUIInputElement>("uui-input, uui-input-password") ?? undefined;
        if (input === this.#formControl) return;

        if (this.#formControl) {
            this.removeFormControlElement(this.#formControl);
        }

        this.#formControl = input;

        if (input) {
            this.addFormControlElement(input);
        }
    }

    override focus() {
        return this.#formControl?.focus();
    }

    /**
     * Re-evaluates whether the current value is a configuration reference, but only while the
     * field is unfocused — swapping the input mid-edit would drop the cursor.
     *
     * `:focus-within` is used rather than the `focusout` event's `relatedTarget` because the
     * reveal toggle lives inside the input's own shadow root, where `relatedTarget` is retargeted.
     */
    #syncReferenceState() {
        if (this.matches(":focus-within")) return;

        this._isReference = isConfigReference(this.value);
    }

    #onFocusOut = () => {
        // Deferred a frame so focus moving between the input and its reveal toggle — which fires
        // focusout before the matching focusin — doesn't read as having left the field.
        requestAnimationFrame(() => this.#syncReferenceState());
    };

    #onInput(e: InputEvent) {
        const newValue = (e.target as HTMLInputElement).value;
        if (newValue === this.value) return;

        this.value = newValue;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const label = this.localize.term("general_fieldFor", [this.name]);

        return this._isReference
            ? html`
                  <uui-input
                      .label=${label}
                      .placeholder=${this._placeholder ?? ""}
                      .requiredMessage=${this.mandatoryMessage}
                      .value=${this.value ?? ""}
                      ?readonly=${this.readonly}
                      ?required=${this.mandatory}
                      spellcheck="false"
                      @input=${this.#onInput}
                  >
                  </uui-input>
              `
            : html`
                  <uui-input-password
                      .label=${label}
                      .placeholder=${this._placeholder ?? ""}
                      .requiredMessage=${this.mandatoryMessage}
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
            uui-input,
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
