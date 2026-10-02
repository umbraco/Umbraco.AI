import { css, html, customElement, repeat, state } from "@umbraco-cms/backoffice/external/lit";
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
import { UAI_DECISION_QUESTION_CONFIG_MODAL } from "./decision-question-config-modal.token.js";
import type { UaiDecisionQuestionKind, UaiDecisionQuestionListItem } from "./types.js";

const elementName = "uai-property-editor-ui-decision-question-list";

/**
 * The questions a kind can be, listed in the "Add question" picker and used for the row detail
 * label (e.g. "Yes/no · refund").
 */
const KIND_ITEMS: Array<{ kind: UaiDecisionQuestionKind; labelKey: string; defaultLabel: string; icon: string }> = [
    { kind: "binary", labelKey: "uaiDecisionQuestionList_kindBinary", defaultLabel: "Yes/no", icon: "icon-check" },
    { kind: "choice", labelKey: "uaiDecisionQuestionList_kindChoice", defaultLabel: "Pick-one", icon: "icon-list" },
    { kind: "score", labelKey: "uaiDecisionQuestionList_kindScore", defaultLabel: "Score", icon: "icon-speed-gauge" },
];

/**
 * Repeatable list of Decision questions, each added through an item picker (choose the kind)
 * then a per-kind config modal — modeled on `uai-guardrail-rule-config-builder`
 * (`UAI_ITEM_PICKER_MODAL` → a config editor modal opened over it, picker stays open until the
 * config modal submits). Value is the flat shape from ARCHITECTURE decision 6
 * (`UaiDecisionQuestionListItem[]`), used by Automate's "Ask questions" action.
 */
@customElement(elementName)
export class UaiPropertyEditorUIDecisionQuestionListElement
    extends UmbFormControlMixin<UaiDecisionQuestionListItem[], typeof UmbLitElement, undefined>(
        UmbLitElement,
        undefined,
    )
    implements UmbPropertyEditorUiElement
{
    override set value(items: UaiDecisionQuestionListItem[] | undefined) {
        this._rows = items ?? [];
    }
    override get value(): UaiDecisionQuestionListItem[] | undefined {
        return this._rows.length > 0 ? this._rows : undefined;
    }

    @state()
    private _rows: UaiDecisionQuestionListItem[] = [];

    @state()
    private _max = 20;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        this._max = config?.getValueByAlias<number>("max") ?? 20;
    }

    constructor() {
        super();

        this.addValidator(
            "rangeOverflow",
            () => this.localize.term("uaiDecisionQuestionList_maxMessage", this._max),
            () => this._rows.length > this._max,
        );
    }

    #kindLabel(kind: UaiDecisionQuestionKind): string {
        const item = KIND_ITEMS.find((k) => k.kind === kind);
        if (!item) return kind;
        return this.localize.termOrDefault(item.labelKey, item.defaultLabel);
    }

    #kindItems(): UaiPickableItemModel[] {
        return KIND_ITEMS.map((item) => ({
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
                title: this.localize.termOrDefault("uaiDecisionQuestionList_pickKind", "Select a kind"),
                autoSubmit: false,
            },
        });

        pickerModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selected = (e as UaiSelectedEvent).item as UaiPickableItemModel;
            const kind = selected.value as UaiDecisionQuestionKind;

            const configModal = modalManager.open(this, UAI_DECISION_QUESTION_CONFIG_MODAL, {
                data: {
                    kind,
                    otherAliases: this.#otherAliases(),
                },
            });

            try {
                const { question } = await configModal.onSubmit();
                pickerModal.reject();
                this._rows = [...this._rows, question];
                this.dispatchEvent(new UmbChangeEvent());
            } catch {
                // Config cancelled — picker remains open so the user can pick a different kind.
            }
        });
    }

    async #onEdit(row: UaiDecisionQuestionListItem, index: number) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const configModal = modalManager.open(this, UAI_DECISION_QUESTION_CONFIG_MODAL, {
            data: {
                kind: row.kind,
                existingQuestion: row,
                otherAliases: this.#otherAliases(row.alias),
            },
        });

        try {
            const { question } = await configModal.onSubmit();
            this._rows = this._rows.map((r, i) => (i === index ? question : r));
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
        return html`
            <uui-ref-list>
                ${repeat(
                    this._rows,
                    // Aliases aren't guaranteed unique while a row is mid-edit (duplicate check only
                    // runs at submit time), so the array index is the only stable repeat key here.
                    (_row, index) => index,
                    (row, index) => html`
                        <uui-ref-node name=${row.instructions} detail="${this.#kindLabel(row.kind)} · ${row.alias}">
                            <umb-icon
                                slot="icon"
                                name=${KIND_ITEMS.find((k) => k.kind === row.kind)?.icon ?? "icon-help-alt"}
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
            <uui-button
                id="btn-add"
                class="add-btn"
                look="placeholder"
                label=${this.localize.termOrDefault("uaiDecisionQuestionList_addQuestion", "Add question")}
                @click=${this.#onAdd}
            >
                <uui-icon name="icon-add"></uui-icon>
                ${this.localize.termOrDefault("uaiDecisionQuestionList_addQuestion", "Add question")}
            </uui-button>
        `;
    }

    static override styles = [
        css`
            .add-btn {
                width: 100%;
            }
        `,
    ];
}

export { UaiPropertyEditorUIDecisionQuestionListElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIDecisionQuestionListElement;
    }
}
