import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { debounceTime, distinctUntilChanged, map } from "@umbraco-cms/backoffice/external/rxjs";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_PROPERTY_DATASET_CONTEXT } from "@umbraco-cms/backoffice/property";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";

const elementName = "uai-property-editor-ui-entity-picker";

@customElement(elementName)
export class UaiPropertyEditorUIEntityPickerElement
    extends UmbFormControlMixin<string | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    @state()
    private _entityType?: string;

    private _entityTypeField = "entityType";

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;
        this._entityTypeField = config.getValueByAlias<string>("entityTypeField") ?? "entityType";
    }

    constructor() {
        super();

        this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, async (context) => {
            if (!context) return;

            const entityType$ = await context.propertyValueByAlias<string>(this._entityTypeField);
            if (!entityType$) return;

            // debounceTime absorbs the brief old-value re-emission during save-rebind,
            // distinctUntilChanged prevents processing when the settled value is unchanged.
            this.observe(
                entityType$.pipe(
                    map((v) => (Array.isArray(v) ? v[0] : v)),
                    debounceTime(50),
                    distinctUntilChanged(),
                ),
                (entityType) => {
                    // Clear only when switching between two different defined types
                    if (this._entityType && entityType && this._entityType !== entityType) {
                        this.value = undefined;
                        this.dispatchEvent(new UmbChangeEvent());
                    }

                    this._entityType = entityType;
                },
                "_observeEntityType",
            );
        });
    }

    #onChange(e: Event) {
        const target = e.target as HTMLElement & { value: string | undefined };
        this.value = target.value || undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        switch (this._entityType) {
            case "document":
                return html`
                    <umb-input-document
                        .value=${this.value ?? ""}
                        .max=${1}
                        ?readonly=${this.readonly}
                        @change=${this.#onChange}>
                    </umb-input-document>
                `;
            case "media":
                return html`
                    <umb-input-media
                        .value=${this.value ?? ""}
                        .max=${1}
                        ?readonly=${this.readonly}
                        @change=${this.#onChange}>
                    </umb-input-media>
                `;
            case "member":
                return html`
                    <umb-input-member
                        .value=${this.value ?? ""}
                        .max=${1}
                        ?readonly=${this.readonly}
                        @change=${this.#onChange}>
                    </umb-input-member>
                `;
            default:
                return html`<uui-label><em>Select an entity type first</em></uui-label>`;
        }
    }
}

export { UaiPropertyEditorUIEntityPickerElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIEntityPickerElement;
    }
}
