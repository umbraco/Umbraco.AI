import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";
import type { UaiEditableModelSchemaModel } from "../../types.js";

/**
 * Event detail for model editor value changes.
 */
export interface UaiModelEditorChangeEventDetail {
    model: Record<string, unknown>;
}

/**
 * Reusable model editor component that renders a dynamic form based on a schema.
 * Uses Umbraco's property dataset and property elements for rendering fields.
 *
 * @fires change - Fired when field values change
 *
 * @example
 * ```html
 * <uai-model-editor
 *   .schema=${providerSchema}
 *   .model=${currentSettings}
 *   @change=${this.#onSettingsChange}>
 * </uai-model-editor>
 * ```
 */
@customElement("uai-model-editor")
export class UaiModelEditorElement extends UmbLitElement {

    /**
     * The schema defining the fields to render.
     */
    @property({ type: Object })
    schema?: UaiEditableModelSchemaModel;

    /**
     * The current model values for the fields (key-value pairs).
     */
    @property({ type: Object })
    model?: Record<string, unknown>;

    /**
     * Placeholder text shown when the schema has no fields.
     */
    @property({ type: String, attribute: "empty-message" })
    emptyMessage?: string;

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    override updated(changedProperties: Map<string, unknown>) {
        if (changedProperties.has("schema") || changedProperties.has("model")) {
            this.#populatePropertyValues();
        }
    }

    #populatePropertyValues() {
        if (!this.schema) {
            this._propertyValues = [];
            return;
        }
        this._propertyValues = this.schema.fields.map((field) => ({
            alias: field.key,
            value: this.model?.[field.key] ?? field.defaultValue,
        }));
    }

    #onChange(e: Event) {
        const dataset = e.target as UmbPropertyDatasetElement;
        const model = dataset.value.reduce(
            (acc, curr) => ({ ...acc, [curr.alias]: curr.value }),
            {} as Record<string, unknown>
        );
        this.dispatchEvent(new CustomEvent<UaiModelEditorChangeEventDetail>(
            "change",
            {
                detail: { model },
                bubbles: true,
                composed: true
            }
        ));
    }

    #toPropertyConfig(config: unknown): Array<{ alias: string; value: unknown }> {
        if (!config) return [];
        // If it's already an array of alias-value pairs, return as is
        if (Array.isArray(config)) return config as Array<{ alias: string; value: unknown }>;
        // If it's an object, convert its entries to alias-value pairs
        if (typeof config !== "object") return [];
        return Object.entries(config).map(([alias, value]) => ({ alias, value }));
    }

    override render() {
        if (!this.schema) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this.schema.fields.length === 0) {
            return html`
                <p class="placeholder-text">
                    ${this.emptyMessage ?? "No configurable fields."}
                </p>
            `;
        }

        return html`
            <umb-property-dataset .value=${this._propertyValues} @change=${this.#onChange}>
                ${this.schema.fields.map(
                    (field) => html`
                        <umb-property
                            label=${this.localize.string(field.label)}
                            description=${this.localize.string(field.description ?? "")}
                            alias=${field.key}
                            property-editor-ui-alias=${field.editorUiAlias ?? "Umb.PropertyEditorUi.TextBox"}
                            .config=${field.editorConfig ? this.#toPropertyConfig(field.editorConfig) : []}
                            ?mandatory=${field.isRequired}>
                        </umb-property>
                    `
                )}
            </umb-property-dataset>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .placeholder-text {
                margin: 0;
                padding: var(--uui-size-space-5) 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
        `,
    ];
}

export default UaiModelEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-model-editor": UaiModelEditorElement;
    }
}
