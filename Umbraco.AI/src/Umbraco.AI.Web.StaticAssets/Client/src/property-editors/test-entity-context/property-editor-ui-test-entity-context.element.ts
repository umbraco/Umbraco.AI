import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import { fetchEntityTypes, fetchEntitySubTypes } from "./test-entity-context.api.js";
import type { TestEntityTypeModel, TestEntitySubTypeModel, EntityContextValue } from "./types.js";

const elementName = "uai-property-editor-ui-test-entity-context";

/**
 * Composite property editor for test entity context.
 * Combines entity type picker, sub-type picker, and mock entity editor.
 */
@customElement(elementName)
export class UaiPropertyEditorUITestEntityContextElement
    extends UmbFormControlMixin<string | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    @state()
    private _entityTypes: TestEntityTypeModel[] = [];

    @state()
    private _subTypes: TestEntitySubTypeModel[] = [];

    @state()
    private _selectedEntityType = "document";

    @state()
    private _selectedSubType?: string;

    @state()
    private _mockEntityJson?: string;

    @state()
    private _loading = false;

    @state()
    private _hasSubTypes = false;

    connectedCallback() {
        super.connectedCallback();
        this.#loadEntityTypes();
        this.#parseValue();
    }

    async #loadEntityTypes() {
        try {
            this._entityTypes = await fetchEntityTypes();
        } catch {
            this._entityTypes = [];
        }
    }

    #parseValue() {
        if (!this.value) return;

        try {
            const parsed: EntityContextValue = JSON.parse(this.value);
            this._selectedEntityType = parsed.entityType ?? "document";
            this._selectedSubType = parsed.entitySubType ?? undefined;
            this._mockEntityJson = parsed.mockEntity ? JSON.stringify(parsed.mockEntity, null, 2) : undefined;

            // Load sub-types for the selected entity type
            const entityTypeInfo = this._entityTypes.find(
                (et) => et.entityType === this._selectedEntityType
            );
            if (entityTypeInfo?.hasSubTypes) {
                this._hasSubTypes = true;
                this.#loadSubTypes(this._selectedEntityType);
            }
        } catch {
            // Invalid JSON - reset
        }
    }

    async #loadSubTypes(entityType: string) {
        this._loading = true;
        try {
            this._subTypes = await fetchEntitySubTypes(entityType);
        } catch {
            this._subTypes = [];
        }
        this._loading = false;
    }

    #onEntityTypeChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this._selectedEntityType = select.value;
        this._selectedSubType = undefined;
        this._mockEntityJson = undefined;

        const entityTypeInfo = this._entityTypes.find(
            (et) => et.entityType === this._selectedEntityType
        );
        this._hasSubTypes = entityTypeInfo?.hasSubTypes ?? false;

        if (this._hasSubTypes) {
            this.#loadSubTypes(this._selectedEntityType);
        } else {
            this._subTypes = [];
        }

        this.#emitValue();
    }

    #onSubTypeChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this._selectedSubType = select.value || undefined;
        this._mockEntityJson = undefined;
        this.#emitValue();
    }

    #onMockEntityChange(e: Event) {
        const target = e.target as HTMLElement & { value?: string };
        this._mockEntityJson = target.value ?? undefined;
        this.#emitValue();
    }

    #onClearMockEntity() {
        this._mockEntityJson = undefined;
        this.#emitValue();
    }

    #emitValue() {
        let mockEntity: Record<string, unknown> | null = null;
        if (this._mockEntityJson) {
            try {
                mockEntity = JSON.parse(this._mockEntityJson);
            } catch {
                // Keep null if invalid JSON
            }
        }

        const contextValue: EntityContextValue = {
            entityType: this._selectedEntityType,
            entitySubType: this._selectedSubType ?? null,
            mockEntity,
        };

        this.value = JSON.stringify(contextValue);
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <div class="entity-context-editor">
                ${this.#renderEntityTypePicker()}
                ${this._hasSubTypes ? this.#renderSubTypePicker() : nothing}
                ${this.#renderMockEntityEditor()}
            </div>
        `;
    }

    #renderEntityTypePicker() {
        return html`
            <umb-property-layout label="Entity Type" description="Type of entity to mock">
                <uui-select
                    slot="editor"
                    .value=${this._selectedEntityType}
                    .options=${this._entityTypes.map((et) => ({
                        name: et.name,
                        value: et.entityType,
                        selected: et.entityType === this._selectedEntityType,
                    }))}
                    ?disabled=${this.readonly}
                    @change=${this.#onEntityTypeChange}>
                </uui-select>
            </umb-property-layout>
        `;
    }

    #renderSubTypePicker() {
        if (this._loading) {
            return html`
                <umb-property-layout label="Sub-Type" description="Loading sub-types...">
                    <uui-loader slot="editor"></uui-loader>
                </umb-property-layout>
            `;
        }

        return html`
            <umb-property-layout label="Sub-Type" description="Select a content type">
                <uui-select
                    slot="editor"
                    .value=${this._selectedSubType ?? ""}
                    .options=${[
                        { name: "-- Select --", value: "", selected: !this._selectedSubType },
                        ...this._subTypes.map((st) => ({
                            name: st.name,
                            value: st.alias,
                            selected: st.alias === this._selectedSubType,
                        })),
                    ]}
                    ?disabled=${this.readonly}
                    @change=${this.#onSubTypeChange}>
                </uui-select>
            </umb-property-layout>
        `;
    }

    #renderMockEntityEditor() {
        const hasMockEntity = !!this._mockEntityJson;

        if (hasMockEntity) {
            return html`
                <umb-property-layout label="Mock Entity" description="AISerializedEntity JSON data">
                    <div slot="editor">
                        <uai-json-mock-entity-editor
                            .entityType=${this._selectedEntityType}
                            .subType=${this._selectedSubType}
                            .value=${this._mockEntityJson}
                            ?readonly=${this.readonly}
                            @change=${this.#onMockEntityChange}>
                        </uai-json-mock-entity-editor>
                        ${!this.readonly
                            ? html`<uui-button
                                look="secondary"
                                label="Clear"
                                @click=${this.#onClearMockEntity}>
                            </uui-button>`
                            : nothing}
                    </div>
                </umb-property-layout>
            `;
        }

        return html`
            <umb-property-layout label="Mock Entity" description="AISerializedEntity JSON data">
                <div slot="editor">
                    <uai-json-mock-entity-editor
                        .entityType=${this._selectedEntityType}
                        .subType=${this._selectedSubType}
                        ?readonly=${this.readonly}
                        @change=${this.#onMockEntityChange}>
                    </uai-json-mock-entity-editor>
                </div>
            </umb-property-layout>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }
        .entity-context-editor {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-4);
        }
    `;
}

export { UaiPropertyEditorUITestEntityContextElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUITestEntityContextElement;
    }
}
