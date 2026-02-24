import { css, customElement, html, nothing, property, state, type PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestEntityTypeResponseModel, TestEntitySubTypeResponseModel } from "../../../api/types.gen.js";
import {
    UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
    type ManifestTestMockEntityEditor,
} from "./mock-entity-editor-extension-type.js";
import { UAI_MOCK_ENTITY_EDITOR_MODAL } from "./mock-entity-editor-modal.token.js";
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
 * When a registered `uaiTestMockEntityEditor` extension exists for the selected
 * entity type, it renders a summary card with Create/Edit/Clear actions using a modal.
 * Otherwise falls back to the inline JSON editor.
 *
 * @fires change - Fires when the entity context value changes (UmbChangeEvent).
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
    private _selectedEntityType = "";

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

    get #hasRegisteredEditor(): boolean {
        if (!this._selectedEntityType) return false;
        const extensions = umbExtensionsRegistry.getByType(
            UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
        ) as ManifestTestMockEntityEditor[];
        return extensions.some((ext) =>
            ext.forEntityTypes.includes(this._selectedEntityType),
        );
    }

    get #selectedSubTypeUnique(): string | undefined {
        if (!this._selectedSubType) return undefined;
        return this._subTypes.find((st) => st.alias === this._selectedSubType)?.unique ?? undefined;
    }

    get #selectedSubTypeName(): string | undefined {
        if (!this._selectedSubType) return undefined;
        return this._subTypes.find((st) => st.alias === this._selectedSubType)?.name ?? undefined;
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
            this._selectedEntityType = parsed.entityType ?? "";
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

    async #onCreateOrEditMockEntity() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_MOCK_ENTITY_EDITOR_MODAL, {
            data: {
                entityType: this._selectedEntityType,
                subTypeAlias: this._selectedSubType,
                subTypeUnique: this.#selectedSubTypeUnique,
                subTypeName: this.#selectedSubTypeName,
                existingValue: this._mockEntityJson,
            },
        });

        try {
            const result = await modal.onSubmit();
            if (result?.mockEntityJson) {
                this._mockEntityJson = result.mockEntityJson;
                this.#emitValue();
            }
        } catch {
            // Modal cancelled
        }
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

    get #showMockEntityEditor(): boolean {
        if (!this._selectedEntityType) return false;
        if (this._hasSubTypes && !this._selectedSubType) return false;
        return true;
    }

    override render() {
        return html`
            <div class="entity-context-editor">
                ${this.#renderEntityTypePicker()}
                ${this._hasSubTypes ? this.#renderSubTypePicker() : nothing}
                ${this.#showMockEntityEditor ? this.#renderMockEntityEditor() : nothing}
            </div>
        `;
    }

    #renderEntityTypePicker() {
        return html`
            <umb-property-layout label="Entity Type" description="Type of entity to mock" style="padding-top: 0;">
                <uui-select
                    slot="editor"
                    .value=${this._selectedEntityType}
                    .options=${[
                        { name: "-- Select --", value: "", selected: !this._selectedEntityType },
                        ...this._entityTypes.map((et) => ({
                            name: et.name,
                            value: et.entityType,
                            selected: et.entityType === this._selectedEntityType,
                        })),
                    ]}
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
        if (this.#hasRegisteredEditor) {
            return this.#renderRegisteredEditorUI();
        }
        return this.#renderJsonEditorFallback();
    }

    #renderRegisteredEditorUI() {
        const hasMockEntity = !!this._mockEntityJson;

        if (!hasMockEntity) {
            return html`
                <umb-property-layout label="Mock Entity" description="Create a mock entity using the structured editor">
                    <div slot="editor">
                        <uui-button
                            look="placeholder"
                            label="Create Mock Entity"
                            ?disabled=${this.readonly}
                            @click=${this.#onCreateOrEditMockEntity}
                        >
                            Create Mock Entity
                        </uui-button>
                    </div>
                </umb-property-layout>
            `;
        }

        // Parse the mock entity for a summary
        const summary = this.#getMockEntitySummary();

        return html`
            <umb-property-layout label="Mock Entity" description="Structured mock entity data">
                <div slot="editor">
                    <uui-ref-node
                        name=${summary.name || "Unnamed Entity"}
                        detail=${summary.detail}
                    >
                        ${!this.readonly
                            ? html`
                                <uui-action-bar slot="actions">
                                    <uui-button
                                        label="Edit"
                                        @click=${this.#onCreateOrEditMockEntity}
                                    ></uui-button>
                                    <uui-button
                                        label="Clear"
                                        color="danger"
                                        @click=${this.#onClearMockEntity}
                                    ></uui-button>
                                </uui-action-bar>`
                            : nothing}
                    </uui-ref-node>
                </div>
            </umb-property-layout>
        `;
    }

    #getMockEntitySummary(): { name: string; detail: string } {
        if (!this._mockEntityJson) return { name: "", detail: "" };

        try {
            const entity = JSON.parse(this._mockEntityJson);
            const name = entity.name ?? "";
            const propCount = entity.data?.properties?.length ?? 0;
            const detail = `${this._selectedSubType ?? entity.entityType} - ${propCount} propert${propCount === 1 ? "y" : "ies"}`;
            return { name, detail };
        } catch {
            return { name: "Invalid JSON", detail: "" };
        }
    }

    #renderJsonEditorFallback() {
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
