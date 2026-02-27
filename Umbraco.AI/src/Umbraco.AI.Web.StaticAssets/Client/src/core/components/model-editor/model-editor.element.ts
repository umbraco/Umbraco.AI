import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";
import type { UaiEditableModelFieldModel, UaiEditableModelSchemaModel } from "../../types.js";

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

    /**
     * Placeholder text shown when the schema has no fields.
     */
    @property({ type: String, attribute: "default-group" })
    defaultGroup?: string;

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    /**
     * Tracks whether we've done the initial population for the current schema.
     * This prevents re-populating on every model change which would reset cursor position.
     */
    #isInitialized = false;

    /**
     * Tracks the last model we emitted via the change event.
     * Used to distinguish between echo updates (parent reflecting our change back)
     * and external updates (loading different data, reset, etc.).
     */
    #lastEmittedModel: Record<string, unknown> | null = null;

    override shouldUpdate(changedProperties: Map<string, unknown>): boolean {
        // After initial population, check if model change is just an echo of our own change
        if (this.#isInitialized && changedProperties.size === 1 && changedProperties.has("model")) {
            // Compare with last emitted model - if it matches, skip re-render (echo update)
            // If it differs, allow re-render (external update like loading different data)
            if (this.#isModelEchoUpdate(this.model)) {
                return false;
            }

            // External change - reset initialization to re-populate
            this.#isInitialized = false;
        }
        return true;
    }

    /**
     * Checks if the incoming model matches what we last emitted (echo update).
     */
    #isModelEchoUpdate(incomingModel: Record<string, unknown> | undefined): boolean {
        if (!this.#lastEmittedModel || !incomingModel) {
            return false;
        }

        const lastKeys = Object.keys(this.#lastEmittedModel);
        const incomingKeys = Object.keys(incomingModel);

        if (lastKeys.length !== incomingKeys.length) {
            return false;
        }

        return lastKeys.every((key) => this.#lastEmittedModel![key] === incomingModel[key]);
    }

    override updated(changedProperties: Map<string, unknown>) {
        // Only re-populate when schema changes (new form structure) or on first load.
        if (changedProperties.has("schema")) {
            this.#isInitialized = false;
            this.#lastEmittedModel = null;
        }

        if (!this.#isInitialized && this.schema) {
            this.#populatePropertyValues();
            this.#isInitialized = true;
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
            {} as Record<string, unknown>,
        );

        // Track emitted model to detect echo updates vs external changes
        this.#lastEmittedModel = model;

        this.dispatchEvent(
            new CustomEvent<UaiModelEditorChangeEventDetail>("change", {
                detail: { model },
                bubbles: true,
                composed: true,
            }),
        );
    }

    #toPropertyConfig(config: unknown): Array<{ alias: string; value: unknown }> {
        if (!config) return [];
        // If it's already an array of alias-value pairs, return as is
        if (Array.isArray(config)) return config as Array<{ alias: string; value: unknown }>;
        // If it's an object, convert its entries to alias-value pairs
        if (typeof config !== "object") return [];
        return Object.entries(config).map(([alias, value]) => ({ alias, value }));
    }

    /**
     * Groups fields by their group property.
     * Fields without a group are placed in "General". General always appears first,
     * followed by named groups in declaration order.
     */
    #groupFields(fields: UaiEditableModelFieldModel[]): Array<[string, UaiEditableModelFieldModel[]]> {
        const generalKey = this.defaultGroup ?? "#uaiFieldGroups_generalLabel";
        const groups = new Map<string, UaiEditableModelFieldModel[]>();
        groups.set(generalKey, []);

        for (const field of fields) {
            const key = field.group ?? generalKey;
            if (!groups.has(key)) {
                groups.set(key, []);
            }
            groups.get(key)!.push(field);
        }

        // Remove General if empty (all fields have explicit groups)
        if (groups.get(generalKey)!.length === 0) {
            groups.delete(generalKey);
        }

        return Array.from(groups.entries());
    }

    #renderField(field: UaiEditableModelFieldModel) {
        return html`
            <umb-property
                label=${this.localize.string(field.label)}
                description=${this.localize.string(field.description ?? "")}
                alias=${field.key}
                property-editor-ui-alias=${field.editorUiAlias ?? "Umb.PropertyEditorUi.TextBox"}
                .config=${field.editorConfig ? this.#toPropertyConfig(field.editorConfig) : []}
                .validation=${{
                    mandatory: field.isRequired,
                    mandatoryMessage: field.isRequired
                        ? this.localize.string("This field is required")
                        : undefined,
                }}
            >
            </umb-property>
        `;
    }

    override render() {
        if (!this.schema) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this.schema.fields.length === 0) {
            return html` <p class="placeholder-text">${this.emptyMessage ?? "No configurable fields."}</p> `;
        }

        return html`
            <umb-property-dataset .value=${this._propertyValues} @change=${this.#onChange}>
                ${this.#groupFields(this.schema.fields).map(
                    ([groupKey, fields]) =>
                        html`<uui-box headline=${this.localize.string(groupKey)}>
                            ${fields.map((f) => this.#renderField(f))}
                        </uui-box>`,
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

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
                margin-top: var(--uui-size-layout-1);
            }

            uui-box:first-child {
                margin-top: 0;
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
