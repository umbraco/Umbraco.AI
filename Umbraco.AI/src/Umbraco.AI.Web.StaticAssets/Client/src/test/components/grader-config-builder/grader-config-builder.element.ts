import { html, customElement, property, state, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { UaiSelectedEvent } from "../../../core/events/selected.event.js";
import { UAI_GRADER_CONFIG_EDITOR_MODAL } from "../../modals/grader-config-editor/index.js";
import type { UaiTestGraderConfig } from "../../types.js";
import { getGraderSummary } from "../../types.js";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestGraderInfoModel } from "../../../api/types.gen.js";

@customElement("uai-grader-config-builder")
export class UaiGraderConfigBuilderElement extends UmbLitElement {
    @property({ type: Array })
    graders: UaiTestGraderConfig[] = [];

    @state()
    private _graders: TestGraderInfoModel[] = [];

    override async connectedCallback() {
        super.connectedCallback();
        await this.#fetchGraders();
    }

    async #fetchGraders() {
        try {
            const { data } = await TestsService.getAllTestGraders();
            this._graders = data ?? [];
        } catch (error) {
            console.error("Failed to fetch grader types:", error);
        }
    }

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        // Open grader type picker
        const typeModal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#getGraderItems(),
                selectionMode: "single",
                title: "Select Grader",
            },
        });

        // Listen for selection event (picker stays open)
        typeModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selectedEvent = e as UaiSelectedEvent;
            const selectedType = selectedEvent.item as UaiPickableItemModel;

            // Open config editor over the picker (picker stays open)
            const configModal = modalManager.open(this, UAI_GRADER_CONFIG_EDITOR_MODAL, {
                data: {
                    graderTypeId: selectedType.value,
                    graderTypeName: selectedType.label,
                    existingGrader: undefined,
                },
            });

            try {
                const configResult = await configModal.onSubmit();

                // Config submitted - close picker and add grader
                typeModal.reject();
                this.graders = [...this.graders, configResult.grader];
                this.dispatchEvent(new UmbChangeEvent());
            } catch {
                // Config cancelled - picker remains open so user can select different type
                // Don't close picker, just let them choose again
            }
        });
    }

    async #onEdit(grader: UaiTestGraderConfig) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        // Get grader type name
        const typeName = this._graders.find((t) => t.id === grader.graderTypeId)?.name;

        // Open config editor directly (skip type picker)
        const modal = modalManager.open(this, UAI_GRADER_CONFIG_EDITOR_MODAL, {
            data: {
                graderTypeId: grader.graderTypeId,
                graderTypeName: typeName || grader.graderTypeId,
                existingGrader: grader,
            },
        });

        try {
            const result = await modal.onSubmit();

            // Update in list and notify
            this.graders = this.graders.map((g) => (g.id === grader.id ? result.grader : g));
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // User cancelled
        }
    }

    #onRemove(graderId: string) {
        this.graders = this.graders.filter((g) => g.id !== graderId);
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #getGraderItems(): Promise<UaiPickableItemModel[]> {
        return this._graders.map((type) => ({
            value: type.id,
            label: type.name,
            description: type.description || undefined,
            meta: { type: type.type },
        }));
    }

    #getGraderDetail(grader: UaiTestGraderConfig): string {
        const typeName = this._graders.find((t) => t.id === grader.graderTypeId)?.name;
        return getGraderSummary(grader, typeName);
    }

    override render() {
        return html`
            <uui-ref-list>
                ${repeat(
                    this.graders,
                    (grader) => grader.id,
                    (grader) => html`
                        <uui-ref-node
                            name=${grader.name || "Unnamed grader"}
                            detail=${this.#getGraderDetail(grader)}
                        >
                            <uui-icon slot="icon" name="icon-check"></uui-icon>
                            <uui-action-bar slot="actions">
                                <uui-button @click=${() => this.#onEdit(grader)} label="Edit">
                                    <uui-icon name="icon-edit"></uui-icon>
                                </uui-button>
                                <uui-button @click=${() => this.#onRemove(grader.id)} label="Remove">
                                    <uui-icon name="icon-trash"></uui-icon>
                                </uui-button>
                            </uui-action-bar>
                        </uui-ref-node>
                    `
                )}
            </uui-ref-list>
            <uui-button look="placeholder" label="Add Grader" @click=${this.#onAdd}>
                <uui-icon name="icon-add"></uui-icon>
                Add Grader
            </uui-button>
        `;
    }
}

export default UaiGraderConfigBuilderElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-grader-config-builder": UaiGraderConfigBuilderElement;
    }
}
