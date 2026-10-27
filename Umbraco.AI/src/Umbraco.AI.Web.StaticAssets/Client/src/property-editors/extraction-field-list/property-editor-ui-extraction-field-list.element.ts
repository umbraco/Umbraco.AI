import { css, html, customElement, repeat, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";
import { UAI_ITEM_PICKER_MODAL } from "../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../core/modals/item-picker/types.js";
import { UaiSelectedEvent } from "../../core/events/selected.event.js";
import { UAI_EXTRACTION_FIELD_CONFIG_MODAL } from "./extraction-field-config-modal.token.js";
import { EXTRACTION_FIELD_KINDS } from "./extraction-field-kinds.js";
import type { UaiExtractionFieldKind, UaiExtractionFieldListItem } from "./types.js";

const elementName = "uai-property-editor-ui-extraction-field-list";

/**
 * Repeatable list of Extract Values fields, each added through an item picker (choose the type)
 * then a per-type config modal — modeled on `uai-property-editor-ui-decision-question-list`.
 * Value is the flat shape `UaiExtractionFieldListItem[]`, read by Automate's "Extract Values"
 * action.
 */
@customElement(elementName)
export class UaiPropertyEditorUIExtractionFieldListElement
    extends UmbFormControlMixin<UaiExtractionFieldListItem[], typeof UmbLitElement, undefined>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    override set value(items: UaiExtractionFieldListItem[] | undefined) {
        this._rows = items ?? [];
    }
    override get value(): UaiExtractionFieldListItem[] | undefined {
        return this._rows.length > 0 ? this._rows : undefined;
    }

    @state()
    private _rows: UaiExtractionFieldListItem[] = [];

    @state()
    private _max = 20;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        this._max = config?.getValueByAlias<number>("max") ?? 20;
    }

    constructor() {
        super();

        this.addValidator(
            "rangeOverflow",
            () => this.localize.term("uaiExtractionFieldList_maxMessage", this._max),
            () => this._rows.length > this._max,
        );
    }

    #kindLabel(kind: UaiExtractionFieldKind): string {
        const item = EXTRACTION_FIELD_KINDS.find((k) => k.kind === kind);
        if (!item) return kind;
        return this.localize.termOrDefault(item.labelKey, item.defaultLabel);
    }

    /** Row detail type: "List of text" for a list, the plain type name otherwise. */
    #rowTypeLabel(row: UaiExtractionFieldListItem): string {
        if (row.kind !== "list") return this.#kindLabel(row.kind);
        switch ((row.itemType ?? "text").toLowerCase()) {
            case "number":
                return this.localize.termOrDefault("uaiExtractionFieldList_listOfNumber", "List of numbers");
            case "date":
                return this.localize.termOrDefault("uaiExtractionFieldList_listOfDate", "List of dates");
            default:
                return this.localize.termOrDefault("uaiExtractionFieldList_listOfText", "List of text");
        }
    }

    #kindItems(): UaiPickableItemModel[] {
        return EXTRACTION_FIELD_KINDS.map((item) => ({
            value: item.kind,
            label: this.localize.termOrDefault(item.labelKey, item.defaultLabel),
            icon: item.icon,
        }));
    }

    #otherAliases(excludeAlias?: string): string[] {
        return this._rows.filter((row) => row.alias !== excludeAlias).map((row) => row.alias);
    }

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const pickerModal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                items: this.#kindItems(),
                selectionMode: "single",
                title: this.localize.termOrDefault("uaiExtractionFieldList_pickKind", "Select a field type"),
                autoSubmit: false,
            },
        });

        pickerModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selected = (e as UaiSelectedEvent).item as UaiPickableItemModel;
            const kind = selected.value as UaiExtractionFieldKind;

            const configModal = modalManager.open(this, UAI_EXTRACTION_FIELD_CONFIG_MODAL, {
                data: {
                    kind,
                    otherAliases: this.#otherAliases(),
                },
            });

            try {
                const { field } = await configModal.onSubmit();
                pickerModal.reject();
                this._rows = [...this._rows, field];
                this.dispatchEvent(new UmbChangeEvent());
            } catch {
                // Config cancelled — picker remains open so the user can pick a different type.
            }
        });
    }

    async #onEdit(row: UaiExtractionFieldListItem, index: number) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const configModal = modalManager.open(this, UAI_EXTRACTION_FIELD_CONFIG_MODAL, {
            data: {
                kind: row.kind,
                existingField: row,
                otherAliases: this.#otherAliases(row.alias),
            },
        });

        try {
            const { field } = await configModal.onSubmit();
            this._rows = this._rows.map((r, i) => (i === index ? field : r));
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // User cancelled.
        }
    }

    #onRemove(index: number) {
        this._rows = this._rows.filter((_, i) => i !== index);
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const addLabel = this.localize.termOrDefault("uaiExtractionFieldList_addField", "Add field");
        return html`
            ${when(
                this._rows.length === 0,
                () => html`
                    <div class="empty">
                        ${this.localize.termOrDefault(
                            "uaiExtractionFieldList_empty",
                            "No fields yet. Add the first value you want to pull out.",
                        )}
                    </div>
                `,
                () => html`
                    <uui-ref-list>
                        ${repeat(
                            this._rows,
                            // Aliases aren't guaranteed unique while a row is mid-edit (duplicate check only
                            // runs at submit time), so the array index is the only stable repeat key here.
                            (_row, index) => index,
                            (row, index) => html`
                                <uui-ref-node
                                    name=${row.instructions}
                                    detail="${this.#rowTypeLabel(row)} · ${row.alias}"
                                    @open=${() => this.#onEdit(row, index)}
                                >
                                    <umb-icon
                                        slot="icon"
                                        name=${EXTRACTION_FIELD_KINDS.find((k) => k.kind === row.kind)?.icon ??
                                        "icon-help-alt"}
                                    ></umb-icon>
                                    <uui-action-bar slot="actions">
                                        <uui-button
                                            @click=${() => this.#onEdit(row, index)}
                                            label=${this.localize.termOrDefault("uaiGeneral_edit", "Edit")}
                                        >
                                            <uui-icon name="icon-edit"></uui-icon>
                                        </uui-button>
                                        <uui-button
                                            @click=${() => this.#onRemove(index)}
                                            label=${this.localize.termOrDefault("uaiGeneral_remove", "Remove")}
                                        >
                                            <uui-icon name="icon-trash"></uui-icon>
                                        </uui-button>
                                    </uui-action-bar>
                                </uui-ref-node>
                            `,
                        )}
                    </uui-ref-list>
                `,
            )}
            <uui-button id="btn-add" class="add-btn" look="placeholder" label=${addLabel} @click=${this.#onAdd}>
                <uui-icon name="icon-add"></uui-icon>
                ${addLabel}
            </uui-button>
        `;
    }

    static override styles = [
        css`
            .add-btn {
                width: 100%;
            }

            .empty {
                padding: var(--uui-size-space-5);
                margin-bottom: var(--uui-size-space-3);
                text-align: center;
                color: var(--uui-color-text-alt);
                border: 1px dashed var(--uui-color-border-standalone);
                border-radius: var(--uui-border-radius);
            }
        `,
    ];
}

export { UaiPropertyEditorUIExtractionFieldListElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIExtractionFieldListElement;
    }
}
