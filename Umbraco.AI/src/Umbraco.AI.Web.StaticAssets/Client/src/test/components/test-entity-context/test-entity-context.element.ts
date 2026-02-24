import { css, customElement, html, nothing, property, state, type PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestEntityTypeResponseModel, TestEntitySubTypeResponseModel } from "../../../api/types.gen.js";
import "./json-mock-entity-editor.element.js";

const elementName = "uai-test-entity-context";

interface EntityContextValue {
    entityType: string;
    entitySubType?: string | null;
    mockEntity?: Record<string, unknown> | null;
}

/**
 * Composite editor for test entity context.
 * Combines entity type picker, sub-type picker, and mock entity editor.
 *
 * @fires change - Fires when the entity context value changes (UmbChangeEvent).
 *
 * @example
 * ```html
 * <uai-test-entity-context
 *   .value=${"{}"}
 *   ?readonly=${false}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-test-entity-context>
 * ```
 */
@customElement(elementName)
export class UaiTestEntityContextElement extends UmbLitElement {
    @property({ type: String })
    value?: string;

    @property({ type: Boolean })
    readonly = false;

    @state()
    private _entityTypes: TestEntityTypeResponseModel[] = [];

    @state()
    private _subTypes: TestEntitySubTypeResponseModel[] = [];

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

    #entityTypesLoaded = false;
    #lastParsedValue?: string;

    connectedCallback() {
        super.connectedCallback();
        this.#loadEntityTypes();
    }

    override willUpdate(changedProperties: PropertyValues) {
        super.willUpdate(changedProperties);

        // Re-sync from value whenever it changes externally and entity types are ready
        if (changedProperties.has("value") && this.#entityTypesLoaded && this.value !== this.#lastParsedValue) {
            this.#syncFromValue();
        }
    }

    async #loadEntityTypes() {
        try {
            const { data } = await TestsService.getAllEntityTypes();
            this._entityTypes = data ?? [];
        } catch {
            this._entityTypes = [];
        }
        this.#entityTypesLoaded = true;
        this.#syncFromValue();
    }

    #syncFromValue() {
        if (!this.value) {
            // No stored value — check if the default entity type has sub-types
            const entityTypeInfo = this._entityTypes.find(
                (et) => et.entityType === this._selectedEntityType
            );
            this._hasSubTypes = entityTypeInfo?.hasSubTypes ?? false;
            if (this._hasSubTypes) {
                this.#loadSubTypes(this._selectedEntityType);
            }
            return;
        }

        try {
            const parsed: EntityContextValue = JSON.parse(this.value);
            this.#lastParsedValue = this.value;
            this._selectedEntityType = parsed.entityType ?? "document";
            this._selectedSubType = parsed.entitySubType ?? undefined;
            this._mockEntityJson = parsed.mockEntity ? JSON.stringify(parsed.mockEntity, null, 2) : undefined;

            const entityTypeInfo = this._entityTypes.find(
                (et) => et.entityType === this._selectedEntityType
            );
            this._hasSubTypes = entityTypeInfo?.hasSubTypes ?? false;
            if (this._hasSubTypes) {
                this.#loadSubTypes(this._selectedEntityType);
            } else {
                this._subTypes = [];
            }
        } catch {
            // Invalid JSON - reset
        }
    }

    async #loadSubTypes(entityType: string) {
        this._loading = true;
        try {
            const { data } = await TestsService.getEntitySubTypes({ path: { entityType } });
            this._subTypes = data ?? [];
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

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiTestEntityContextElement;
    }
}
