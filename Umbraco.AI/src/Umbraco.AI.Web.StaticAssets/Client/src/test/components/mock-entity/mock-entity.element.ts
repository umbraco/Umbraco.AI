import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { UmbModalManagerContext, UmbModalContext } from "@umbraco-cms/backoffice/modal";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { UaiSelectedEvent } from "../../../core/events/selected.event.js";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestEntityTypeResponseModel, TestEntitySubTypeResponseModel } from "../../../api/types.gen.js";
import { UAI_MOCK_ENTITY_EDITOR_MODAL } from "./mock-entity-editor-modal.token.js";

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

            if (entityTypeInfo?.hasSubTypes) {
                this.#openSubTypePicker(modalManager, typeModal, selectedType.value);
            } else {
                this.#openMockEditor(modalManager, [typeModal], selectedType.value);
            }
        });
    }

    #openSubTypePicker(
        modalManager: UmbModalManagerContext,
        typeModal: UmbModalContext,
        entityType: string,
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
                modalManager,
                [typeModal, subTypeModal],
                entityType,
                selectedSubType.value,
                selectedSubType.meta?.unique,
                selectedSubType.label,
            );
        });
    }

    async #openMockEditor(
        modalManager: UmbModalManagerContext,
        parentModals: UmbModalContext[],
        entityType: string,
        subTypeAlias?: string,
        subTypeUnique?: string,
        subTypeName?: string,
    ) {
        const editorModal = modalManager.open(this, UAI_MOCK_ENTITY_EDITOR_MODAL, {
            data: {
                entityType,
                subTypeAlias,
                subTypeUnique,
                subTypeName,
            },
        });

        try {
            const result = await editorModal.onSubmit();
            // Close all parent modals
            for (const modal of parentModals) {
                modal.reject();
            }
            this.#saveValue(entityType, subTypeAlias, result.mockEntityJson);
        } catch {
            // Editor cancelled - parent modals stay open
        }
    }

    async #onEdit() {
        if (!this.value) return;

        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

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

        const modal = modalManager.open(this, UAI_MOCK_ENTITY_EDITOR_MODAL, {
            data: {
                entityType: this.value.entityType,
                subTypeAlias: this.value.entitySubType ?? undefined,
                subTypeUnique,
                subTypeName,
                existingValue: this.value.mockEntity
                    ? JSON.stringify(this.value.mockEntity, null, 2)
                    : undefined,
            },
        });

        try {
            const result = await modal.onSubmit();
            this.#saveValue(
                this.value.entityType,
                this.value.entitySubType ?? undefined,
                result.mockEntityJson,
            );
        } catch {
            // User cancelled
        }
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

        return html`
            <uui-ref-node
                name=${summary.name || "Unnamed Entity"}
                detail=${summary.detail}
            >
                <umb-icon slot="icon" name=${this.#getMockEntityIcon()}></umb-icon>
                ${!this.readonly
                    ? html`
                          <uui-action-bar slot="actions">
                              <uui-button label="Edit" @click=${this.#onEdit}>
                                  <uui-icon name="icon-edit"></uui-icon>
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
