import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { UmbModalManagerContext, UmbModalContext } from "@umbraco-cms/backoffice/modal";
import { UmbModalRouteRegistrationController } from "@umbraco-cms/backoffice/router";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { UaiSelectedEvent } from "../../../core/events/selected.event.js";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestEntityTypeResponseModel, TestEntitySubTypeResponseModel } from "../../../api/types.gen.js";
import { UAI_MOCK_ENTITY_EDITOR_MODAL } from "./mock-entity-editor-modal.token.js";
import type { UaiMockEntityEditorModalData } from "./mock-entity-editor-modal.token.js";
import {
    UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
    type ManifestTestMockEntityEditor,
} from "./mock-entity-editor-extension-type.js";

const elementName = "uai-mock-entity";

export interface EntityContextValue {
    entityType: string;
    entitySubType?: string | null;
    mockEntity?: Record<string, unknown> | null;
}

/**
 * Mock entity builder component using a multi-modal workflow.
 *
 * Flow:
 * 1. "Create Mock" button opens an entity type picker modal.
 * 2. If the entity type supports sub-types, a second picker modal opens
 *    on top to select the sub-type.
 * 3. After type (and optional sub-type) selection, the mock entity editor
 *    modal opens for data entry.
 * 4. All modals remain open until the final submit, allowing the user to
 *    cancel any modal to go back and pick a different option.
 *
 * Once completed, a summary card is shown with Edit and Delete actions.
 *
 * @fires change - Fires when the mock entity value changes (UmbChangeEvent).
 */
@customElement(elementName)
export class UaiMockEntityElement extends UmbLitElement {
    @property({ type: Object })
    value?: EntityContextValue;

    @property({ type: Boolean })
    readonly = false;

    @state()
    private _entityTypes: TestEntityTypeResponseModel[] = [];

    #entityTypesLoaded = false;

    /**
     * Route path for the editor modal, emitted by UmbModalRouteRegistrationController
     * once the route is registered (ready shortly after construction).
     */
    #editorRoutePath?: string;

    /**
     * Pending modal data, captured when the user finishes the picker chain (or clicks
     * Edit/Edit JSON) and handed to the route-registered modal via its onSetup callback.
     * Cleared when the modal resolves or is cancelled.
     */
    #pendingEditorData?: UaiMockEntityEditorModalData;

    /**
     * In-flight flow metadata for the current editor invocation. Tracks which picker
     * modals to close on submit and which entity type/sub-type to save the result
     * against. Only lives between open-the-editor and submit/cancel.
     */
    #pendingEditorFlow?: {
        parentModals: UmbModalContext[];
        entityType: string;
        subTypeAlias?: string;
    };

    constructor() {
        super();

        // Register the mock entity editor modal as a route under this element.
        // Opening the editor means navigating to this route via history.pushState,
        // mirroring how CMS workspaces open their block/workspace modals. This gives
        // the inner cms-mock-entity-editor's <umb-router-slot> a clean URL segment
        // to extend (for tab sub-paths) and ensures the URL is tidied up when the
        // modal closes.
        new UmbModalRouteRegistrationController(this, UAI_MOCK_ENTITY_EDITOR_MODAL)
            .addAdditionalPath("mock-entity-editor")
            .onSetup(() => {
                // Guard against stray navigation (e.g. URL replayed without going
                // through the picker chain): refuse to open without pending data.
                if (!this.#pendingEditorData) return false;
                return { data: this.#pendingEditorData };
            })
            .onSubmit((value) => {
                const flow = this.#pendingEditorFlow;
                if (!flow || !value) {
                    this.#resetEditorFlow();
                    return;
                }
                // Close picker chain and persist the resolved value, matching the
                // previous `await onSubmit(); for (m of parentModals) m.reject()` behaviour.
                for (const modal of flow.parentModals) modal.reject();
                this.#saveValue(flow.entityType, flow.subTypeAlias, value.mockEntityJson);
                this.#resetEditorFlow();
            })
            .onReject(() => {
                // Editor cancelled: leave any picker modals open so the user can
                // pick a different type, matching the previous try/catch behaviour.
                this.#resetEditorFlow();
            })
            .observeRouteBuilder((routeBuilder) => {
                this.#editorRoutePath = routeBuilder({});
            });
    }

    connectedCallback() {
        super.connectedCallback();
        this.#loadEntityTypes();
    }

    async #loadEntityTypes() {
        try {
            const { data } = await TestsService.getAllEntityTypes();
            this._entityTypes = data ?? [];
        } catch {
            this._entityTypes = [];
        }
        this.#entityTypesLoaded = true;
    }

    get #hasMockEntity(): boolean {
        return !!this.value?.mockEntity;
    }

    #getEditorManifest(entityType: string): ManifestTestMockEntityEditor | undefined {
        const extensions = umbExtensionsRegistry.getByType(
            UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
        ) as ManifestTestMockEntityEditor[];
        return extensions.find((ext) => ext.forEntityTypes.includes(entityType));
    }

    get #hasCustomEditor(): boolean {
        if (!this.value?.entityType) return false;
        return !!this.#getEditorManifest(this.value.entityType);
    }

    async #onCreate() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const typeModal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#getEntityTypeItems(),
                title: "Select Entity Type",
                autoSubmit: false,
            },
        });

        typeModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selectedType = (e as UaiSelectedEvent).item as UaiPickableItemModel;
            const entityTypeInfo = this._entityTypes.find(
                (et) => et.entityType === selectedType.value,
            );

            const editorManifest = this.#getEditorManifest(selectedType.value);
            if (entityTypeInfo?.hasSubTypes) {
                this.#openSubTypePicker(modalManager, typeModal, selectedType.value, editorManifest);
            } else {
                this.#openMockEditor([typeModal], selectedType.value, undefined, undefined, undefined, editorManifest);
            }
        });
    }

    #openSubTypePicker(
        modalManager: UmbModalManagerContext,
        typeModal: UmbModalContext,
        entityType: string,
        editorManifest?: ManifestTestMockEntityEditor,
    ) {
        const subTypeModal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#getSubTypeItems(entityType),
                title: "Select Sub-Type",
                autoSubmit: false,
            },
        });

        subTypeModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selectedSubType = (e as UaiSelectedEvent).item as UaiPickableItemModel;
            this.#openMockEditor(
                [typeModal, subTypeModal],
                entityType,
                selectedSubType.value,
                selectedSubType.meta?.unique,
                selectedSubType.label,
                editorManifest,
            );
        });
    }

    #openMockEditor(
        parentModals: UmbModalContext[],
        entityType: string,
        subTypeAlias?: string,
        subTypeUnique?: string,
        subTypeName?: string,
        editorManifest?: ManifestTestMockEntityEditor,
    ) {
        this.#pendingEditorFlow = { parentModals, entityType, subTypeAlias };
        this.#pendingEditorData = {
            entityType,
            subTypeAlias,
            subTypeUnique,
            subTypeName,
            editorManifest,
        };
        this.#navigateToEditorRoute();
    }

    async #onEdit() {
        if (!this.value) return;
        const editorManifest = this.#getEditorManifest(this.value.entityType);
        await this.#openExistingEditor(editorManifest);
    }

    async #onEditJson() {
        if (!this.value) return;
        await this.#openExistingEditor(undefined);
    }

    async #openExistingEditor(editorManifest?: ManifestTestMockEntityEditor) {
        if (!this.value) return;

        // Resolve sub-type info if needed
        let subTypeUnique: string | undefined;
        let subTypeName: string | undefined;
        if (this.value.entitySubType) {
            try {
                const { data } = await TestsService.getEntitySubTypes({
                    path: { entityType: this.value.entityType },
                });
                const subType = data?.find((st) => st.alias === this.value!.entitySubType);
                subTypeUnique = subType?.unique ?? undefined;
                subTypeName = subType?.name ?? undefined;
            } catch {
                // Continue without sub-type info
            }
        }

        this.#pendingEditorFlow = {
            parentModals: [],
            entityType: this.value.entityType,
            subTypeAlias: this.value.entitySubType ?? undefined,
        };
        this.#pendingEditorData = {
            entityType: this.value.entityType,
            subTypeAlias: this.value.entitySubType ?? undefined,
            subTypeUnique,
            subTypeName,
            existingValue: this.value.mockEntity
                ? JSON.stringify(this.value.mockEntity, null, 2)
                : undefined,
            editorManifest,
        };
        this.#navigateToEditorRoute();
    }

    #navigateToEditorRoute() {
        if (!this.#editorRoutePath) {
            // Registration runs synchronously in the constructor, but the route
            // builder observable can fire on the next microtask. If this ever
            // happens in practice, surface it loudly so we can investigate.
            console.error("Mock entity editor route path not yet registered.");
            this.#resetEditorFlow();
            return;
        }
        window.history.pushState({}, "", this.#editorRoutePath);
    }

    #resetEditorFlow() {
        this.#pendingEditorData = undefined;
        this.#pendingEditorFlow = undefined;
    }

    #onDelete() {
        this.value = undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    #saveValue(entityType: string, subTypeAlias?: string, mockEntityJson?: string) {
        let mockEntity: Record<string, unknown> | null = null;
        if (mockEntityJson) {
            try {
                mockEntity = JSON.parse(mockEntityJson);
            } catch {
                // Keep null if invalid
            }
        }

        this.value = {
            entityType,
            entitySubType: subTypeAlias ?? null,
            mockEntity,
        };
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #getEntityTypeItems(): Promise<UaiPickableItemModel[]> {
        // Ensure entity types are loaded
        if (!this.#entityTypesLoaded) {
            await this.#loadEntityTypes();
        }
        return this._entityTypes.map((et) => ({
            value: et.entityType,
            label: et.name,
            description: et.hasSubTypes ? "Has sub-types" : undefined,
            icon: et.icon ?? "icon-document",
        }));
    }

    async #getSubTypeItems(entityType: string): Promise<UaiPickableItemModel[]> {
        try {
            const { data } = await TestsService.getEntitySubTypes({
                path: { entityType },
            });
            return (data ?? []).map((st: TestEntitySubTypeResponseModel) => ({
                value: st.alias,
                label: st.name,
                description: st.description ?? undefined,
                icon: st.icon ?? "icon-item-arrangement",
                meta: { unique: st.unique },
            }));
        } catch {
            return [];
        }
    }

    #getMockEntityIcon(): string {
        if (!this.value) return "icon-document";
        return this._entityTypes.find((et) => et.entityType === this.value!.entityType)?.icon ?? "icon-document";
    }

    #getMockEntitySummary(): { name: string; detail: string } {
        if (!this.value?.mockEntity) return { name: "", detail: "" };

        try {
            const entity = this.value.mockEntity;
            const name = (entity.name as string) ?? "";
            const data = entity.data as Record<string, unknown> | undefined;
            const properties = data?.properties as unknown[] | undefined;
            const propCount = properties?.length ?? 0;
            const entityTypeName =
                this._entityTypes.find((et) => et.entityType === this.value!.entityType)?.name ??
                this.value!.entityType;
            const subTypePart = this.value!.entitySubType ? ` (${this.value!.entitySubType})` : "";
            const detail = `${entityTypeName}${subTypePart} - ${propCount} propert${propCount === 1 ? "y" : "ies"}`;
            return { name, detail };
        } catch {
            return { name: "Invalid data", detail: "" };
        }
    }

    override render() {
        if (this.#hasMockEntity) {
            return this.#renderSummaryCard();
        }
        return this.#renderCreateButton();
    }

    #renderCreateButton() {
        return html`
            <uui-button
                class="create-btn"
                look="placeholder"
                label="Add"
                ?disabled=${this.readonly}
                @click=${this.#onCreate}
            >
                <uui-icon name="icon-add"></uui-icon>
                Add
            </uui-button>
        `;
    }

    #renderSummaryCard() {
        const summary = this.#getMockEntitySummary();
        const hasCustomEditor = this.#hasCustomEditor;

        return html`
            <uui-ref-node
                name=${summary.name || "Unnamed Entity"}
                detail=${summary.detail}
            >
                <umb-icon slot="icon" name=${this.#getMockEntityIcon()}></umb-icon>
                ${!this.readonly
                    ? html`
                          <uui-action-bar slot="actions">
                              ${hasCustomEditor
                                  ? html`<uui-button label="Edit" @click=${this.#onEdit}>
                                        <uui-icon name="icon-edit"></uui-icon>
                                    </uui-button>`
                                  : nothing}
                              <uui-button label="Edit JSON" @click=${this.#onEditJson}>
                                  <uui-icon name="icon-code"></uui-icon>
                              </uui-button>
                              <uui-button label="Delete" @click=${this.#onDelete}>
                                  <uui-icon name="icon-trash"></uui-icon>
                              </uui-button>
                          </uui-action-bar>
                      `
                    : nothing}
            </uui-ref-node>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }
        .create-btn {
            width: 100%;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiMockEntityElement;
    }
}
