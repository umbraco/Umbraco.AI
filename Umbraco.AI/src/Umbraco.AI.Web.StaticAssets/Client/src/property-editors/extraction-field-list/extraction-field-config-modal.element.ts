import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    UaiExtractionFieldConfigModalData,
    UaiExtractionFieldConfigModalValue,
} from "./extraction-field-config-modal.token.js";
import { EXTRACTION_FIELD_KINDS } from "./extraction-field-kinds.js";
import type { UaiExtractionDurationUnit, UaiExtractionFieldListItem, UaiExtractionListItemType } from "./types.js";

const elementName = "uai-extraction-field-config-modal";

/** Same rule Core applies to a field id; checking it here keeps the mistake next to the input. */
const ALIAS_PATTERN = /^[A-Za-z_][A-Za-z0-9_]{0,63}$/;

const MAX_DECIMALS = 6;

const DURATION_UNITS: Array<{ value: UaiExtractionDurationUnit; labelKey: string; defaultLabel: string }> = [
    { value: "seconds", labelKey: "uaiExtractionFieldConfigModal_unitSeconds", defaultLabel: "Seconds" },
    { value: "minutes", labelKey: "uaiExtractionFieldConfigModal_unitMinutes", defaultLabel: "Minutes" },
    { value: "hours", labelKey: "uaiExtractionFieldConfigModal_unitHours", defaultLabel: "Hours" },
    { value: "days", labelKey: "uaiExtractionFieldConfigModal_unitDays", defaultLabel: "Days" },
    { value: "weeks", labelKey: "uaiExtractionFieldConfigModal_unitWeeks", defaultLabel: "Weeks" },
    { value: "months", labelKey: "uaiExtractionFieldConfigModal_unitMonths", defaultLabel: "Months" },
    { value: "years", labelKey: "uaiExtractionFieldConfigModal_unitYears", defaultLabel: "Years" },
];

const ITEM_TYPES: Array<{ value: UaiExtractionListItemType; labelKey: string; defaultLabel: string }> = [
    { value: "text", labelKey: "uaiExtractionFieldConfigModal_itemTypeText", defaultLabel: "Text" },
    { value: "number", labelKey: "uaiExtractionFieldConfigModal_itemTypeNumber", defaultLabel: "Number" },
    { value: "date", labelKey: "uaiExtractionFieldConfigModal_itemTypeDate", defaultLabel: "Date" },
];

/**
 * Per-type config editor for one field in the `Uai.PropertyEditorUi.ExtractionFieldList`.
 * Opened either from the item picker (new field) or by clicking a row (edit). Mirrors
 * `uai-decision-question-config-modal` (umb-property-layout fields in a uui-box, submit via a
 * button, cancel via modalContext.reject()), with properties stacked label-on-top.
 */
@customElement(elementName)
export class UaiExtractionFieldConfigModalElement extends UmbModalBaseElement<
    UaiExtractionFieldConfigModalData,
    UaiExtractionFieldConfigModalValue
> {
    @state()
    private _alias = "";

    @state()
    private _instructions = "";

    @state()
    private _decimals = "";

    @state()
    private _integer = false;

    @state()
    private _unit = "";

    @state()
    private _itemType: UaiExtractionListItemType = "text";

    @state()
    private _aliasError?: string;

    @state()
    private _instructionsError?: string;

    @state()
    private _decimalsError?: string;

    override connectedCallback() {
        super.connectedCallback();

        const existing = this.data?.existingField;
        this._alias = existing?.alias ?? "";
        this._instructions = existing?.instructions ?? "";
        this._decimals = existing?.decimals === undefined ? "" : String(existing.decimals);
        this._integer = existing?.integer ?? false;
        this._itemType = existing?.itemType ?? "text";

        // A duration always saves a unit, so it starts on (and falls back to) seconds.
        const unit = existing?.unit ?? "";
        if (this.data?.kind === "duration") {
            const known = DURATION_UNITS.find((u) => u.value === unit.toLowerCase());
            this._unit = known?.value ?? "seconds";
        } else {
            this._unit = unit;
        }
    }

    #onAliasInput(e: Event) {
        this._alias = (e.target as HTMLInputElement).value;
    }

    #onInstructionsInput(e: Event) {
        this._instructions = (e.target as HTMLTextAreaElement).value;
    }

    #onDecimalsInput(e: Event) {
        this._decimals = String((e.target as HTMLInputElement).value ?? "");
    }

    #onIntegerChange(e: Event) {
        this._integer = (e.target as HTMLInputElement).checked;
    }

    #onUnitInput(e: Event) {
        this._unit = String((e.target as HTMLInputElement).value ?? "");
    }

    #onItemTypeChange(e: Event) {
        this._itemType = (e.target as HTMLInputElement).value as UaiExtractionListItemType;
    }

    /** Validates every input and, when valid, builds the flat field to submit. */
    #validate(): UaiExtractionFieldListItem | undefined {
        const kind = this.data!.kind;
        this._aliasError = undefined;
        this._instructionsError = undefined;
        this._decimalsError = undefined;

        const alias = this._alias.trim();
        const otherAliases = this.data?.otherAliases ?? [];
        if (!alias) {
            this._aliasError = this.localize.termOrDefault(
                "uaiExtractionFieldConfigModal_aliasRequired",
                "Alias is required.",
            );
        } else if (!ALIAS_PATTERN.test(alias)) {
            this._aliasError = this.localize.termOrDefault(
                "uaiExtractionFieldConfigModal_aliasInvalidFormat",
                "Alias must start with a letter or _ and use only letters, numbers and _ (up to 64 characters).",
            );
        } else if (otherAliases.includes(alias)) {
            this._aliasError = this.localize.termOrDefault(
                "uaiExtractionFieldConfigModal_aliasDuplicate",
                `Another field already uses the alias "${alias}".`,
                alias,
            );
        }

        const instructions = this._instructions.trim();
        if (!instructions) {
            this._instructionsError = this.localize.termOrDefault(
                "uaiExtractionFieldConfigModal_instructionsRequired",
                "Instructions are required.",
            );
        }

        const hasDecimals = kind === "number" || kind === "duration";
        const decimalsText = this._decimals.trim();
        let decimals: number | undefined;
        if (hasDecimals && decimalsText !== "") {
            decimals = Number(decimalsText);
            if (!Number.isInteger(decimals) || decimals < 0 || decimals > MAX_DECIMALS) {
                this._decimalsError = this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_decimalsRange",
                    "Decimal places must be a whole number from 0 to 6.",
                );
            }
        }

        if (this._aliasError || this._instructionsError || this._decimalsError) return undefined;

        const field: UaiExtractionFieldListItem = { kind, alias, instructions };

        if (hasDecimals) {
            if (decimals !== undefined) field.decimals = decimals;
            field.integer = this._integer;
        }
        if (kind === "duration") {
            field.unit = this._unit;
        } else if (kind === "number" && this._unit.trim()) {
            field.unit = this._unit.trim();
        } else if (kind === "list") {
            field.itemType = this._itemType;
        }

        return field;
    }

    #onSubmit() {
        const field = this.#validate();
        if (!field) return;

        this.value = { field };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    #kindLabel(): string {
        const item = EXTRACTION_FIELD_KINDS.find((k) => k.kind === this.data?.kind);
        return item ? this.localize.termOrDefault(item.labelKey, item.defaultLabel) : (this.data?.kind ?? "");
    }

    override render() {
        const kindLabel = this.#kindLabel();
        const headline = this.data?.existingField
            ? this.localize.termOrDefault(
                  "uaiExtractionFieldConfigModal_headlineEdit",
                  `Edit field: ${kindLabel}`,
                  kindLabel,
              )
            : this.localize.termOrDefault(
                  "uaiExtractionFieldConfigModal_headlineAdd",
                  `Add field: ${kindLabel}`,
                  kindLabel,
              );
        const aliasLabel = this.localize.termOrDefault("uaiLabels_alias", "Alias");
        const instructionsLabel = this.localize.termOrDefault(
            "uaiExtractionFieldConfigModal_instructionsLabel",
            "Instructions",
        );
        const kind = this.data?.kind;

        return html`
            <umb-body-layout headline=${headline}>
                <div>
                    <uui-box>
                        <umb-property-layout
                            orientation="vertical"
                            label=${aliasLabel}
                            description=${this.localize.termOrDefault(
                                "uaiExtractionFieldConfigModal_aliasDescription",
                                "Used to reach this value in later steps. Letters, numbers and _ only.",
                            )}
                        >
                            <!-- The description attribute is rendered as UFM, which swallows "{...}", so the binding example lives in the slot. -->
                            <div slot="description" class="alias-example">
                                ${this.localize.termOrDefault(
                                    "uaiExtractionFieldConfigModal_aliasExampleLabel",
                                    "Example:",
                                )}
                                <code>\${ steps.&lt;stepAlias&gt;.&lt;alias&gt;.value }</code>
                            </div>
                            <div slot="editor">
                                <uui-input
                                    id="alias"
                                    label=${aliasLabel}
                                    .value=${this._alias}
                                    ?error=${!!this._aliasError}
                                    @input=${this.#onAliasInput}
                                ></uui-input>
                                ${when(
                                    this._aliasError,
                                    () => html`<p class="alias-error" role="alert">${this._aliasError}</p>`,
                                )}
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            orientation="vertical"
                            label=${instructionsLabel}
                            description=${this.localize.termOrDefault(
                                "uaiExtractionFieldConfigModal_instructionsDescription",
                                "What the field should contain. Be specific about what you mean.",
                            )}
                        >
                            <div slot="editor">
                                <uui-textarea
                                    id="instructions"
                                    label=${instructionsLabel}
                                    .value=${this._instructions}
                                    ?error=${!!this._instructionsError}
                                    @input=${this.#onInstructionsInput}
                                ></uui-textarea>
                                ${when(
                                    this._instructionsError,
                                    () =>
                                        html`<p class="instructions-error" role="alert">${this._instructionsError}</p>`,
                                )}
                            </div>
                        </umb-property-layout>

                        ${kind === "number" ? this.#renderNumberFields() : nothing}
                        ${kind === "duration" ? this.#renderDurationFields() : nothing}
                        ${kind === "list" ? this.#renderListFields() : nothing}
                    </uui-box>
                </div>

                <div slot="actions">
                    <uui-button
                        id="btn-cancel"
                        label=${this.localize.termOrDefault("general_cancel", "Cancel")}
                        @click=${this.#onCancel}
                    ></uui-button>
                    <uui-button
                        id="btn-submit"
                        look="primary"
                        color="positive"
                        label=${this.localize.termOrDefault("buttons_save", "Save")}
                        @click=${this.#onSubmit}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    #renderDecimals() {
        const label = this.localize.termOrDefault("uaiExtractionFieldConfigModal_decimalsLabel", "Decimal places");
        return html`
            <umb-property-layout
                orientation="vertical"
                label=${label}
                description=${this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_decimalsDescription",
                    "Optional. 0 to 6.",
                )}
            >
                <div slot="editor">
                    <uui-input
                        id="decimals"
                        class="narrow"
                        type="number"
                        label=${label}
                        min="0"
                        max=${MAX_DECIMALS}
                        step="1"
                        .value=${this._decimals}
                        ?error=${!!this._decimalsError}
                        @input=${this.#onDecimalsInput}
                    ></uui-input>
                    ${when(
                        this._decimalsError,
                        () => html`<p class="decimals-error" role="alert">${this._decimalsError}</p>`,
                    )}
                </div>
            </umb-property-layout>
        `;
    }

    #renderInteger() {
        const label = this.localize.termOrDefault("uaiExtractionFieldConfigModal_integerLabel", "Whole number");
        return html`
            <umb-property-layout
                orientation="vertical"
                label=${label}
                description=${this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_integerDescription",
                    "Only accept whole numbers.",
                )}
            >
                <div slot="editor">
                    <uui-toggle
                        id="integer"
                        label=${label}
                        .checked=${this._integer}
                        @change=${this.#onIntegerChange}
                    ></uui-toggle>
                </div>
            </umb-property-layout>
        `;
    }

    #renderNumberFields() {
        const unitLabel = this.localize.termOrDefault("uaiExtractionFieldConfigModal_numberUnitLabel", "Unit");
        return html`
            ${this.#renderDecimals()} ${this.#renderInteger()}
            <umb-property-layout
                orientation="vertical"
                label=${unitLabel}
                description=${this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_numberUnitDescription",
                    "Optional. Helps the model pick the right number, e.g. EUR or kg.",
                )}
            >
                <div slot="editor">
                    <uui-input
                        id="unit"
                        class="narrow"
                        label=${unitLabel}
                        .value=${this._unit}
                        @input=${this.#onUnitInput}
                    ></uui-input>
                </div>
            </umb-property-layout>
        `;
    }

    #renderDurationFields() {
        const unitLabel = this.localize.termOrDefault("uaiExtractionFieldConfigModal_numberUnitLabel", "Unit");
        return html`
            <umb-property-layout
                orientation="vertical"
                label=${unitLabel}
                description=${this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_durationUnitDescription",
                    "The unit the duration is measured in.",
                )}
            >
                <div slot="editor">
                    <uui-select
                        id="unit"
                        label=${unitLabel}
                        .options=${DURATION_UNITS.map((u) => ({
                            name: this.localize.termOrDefault(u.labelKey, u.defaultLabel),
                            value: u.value,
                            selected: u.value === this._unit,
                        }))}
                        .value=${this._unit}
                        @change=${this.#onUnitInput}
                    ></uui-select>
                </div>
            </umb-property-layout>
            ${this.#renderDecimals()} ${this.#renderInteger()}
        `;
    }

    #renderListFields() {
        const label = this.localize.termOrDefault("uaiExtractionFieldConfigModal_itemTypeLabel", "Item type");
        return html`
            <umb-property-layout
                orientation="vertical"
                label=${label}
                description=${this.localize.termOrDefault(
                    "uaiExtractionFieldConfigModal_itemTypeDescription",
                    "What each item in the list is.",
                )}
            >
                <div slot="editor">
                    <uui-select
                        id="item-type"
                        label=${label}
                        .options=${ITEM_TYPES.map((t) => ({
                            name: this.localize.termOrDefault(t.labelKey, t.defaultLabel),
                            value: t.value,
                            selected: t.value === this._itemType,
                        }))}
                        .value=${this._itemType}
                        @change=${this.#onItemTypeChange}
                    ></uui-select>
                </div>
            </umb-property-layout>
        `;
    }

    static override styles = [
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-input,
            uui-textarea {
                width: 100%;
            }

            uui-input.narrow {
                width: 200px;
            }

            .alias-example {
                color: var(--uui-color-text-alt);
            }

            .alias-error,
            .instructions-error,
            .decimals-error {
                color: var(--uui-color-danger);
                font-size: 0.85em;
                margin: var(--uui-size-space-2) 0 0;
            }
        `,
    ];
}

export { UaiExtractionFieldConfigModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiExtractionFieldConfigModalElement;
    }
}
