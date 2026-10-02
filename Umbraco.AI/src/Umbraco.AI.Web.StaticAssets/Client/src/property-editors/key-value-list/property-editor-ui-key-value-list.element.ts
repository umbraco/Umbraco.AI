import { css, customElement, html, nothing, property, repeat, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbSorterController } from "@umbraco-cms/backoffice/sorter";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";

const elementName = "uai-property-editor-ui-key-value-list";

/**
 * A single key/value row's public shape — what the editor's value actually is.
 */
export interface UaiKeyValueListItem {
    key: string;
    value: string;
}

/** A row plus a stable identity for the sorter and for tracking edits, never part of the emitted value. */
interface UaiKeyValueListRow extends UaiKeyValueListItem {
    id: string;
}

/**
 * Repeatable key/value list editor: one row per entry, each with a key and a value text input,
 * plus add, remove, and drag-to-reorder. Modeled on the CMS `Umb.PropertyEditorUi.MultipleTextString`
 * (rows, add button, per-row remove, `UmbSorterController` reordering, readonly support, min/max
 * validation) — but with two text inputs per row instead of one, since the value is
 * `Array<{ key, value }>` rather than a flat string list.
 *
 * Row identity (`UaiKeyValueListRow.id`) is a `crypto.randomUUID()` assigned once per row and never
 * derived from its content, unlike `MultipleTextString`'s use of the row's own string value as its
 * sort id. That lets two rows share the same key or value mid-edit (e.g. both freshly added and
 * still blank) without confusing the sorter or losing input focus while typing.
 */
@customElement(elementName)
export class UaiPropertyEditorUIKeyValueListElement
    extends UmbFormControlMixin<UaiKeyValueListItem[], typeof UmbLitElement, undefined>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    /**
     * Single underscore (not `#private`) so a spec can reach it directly to assert the sorter's
     * own stored model — the same convention already used for `_rows`/`_min`/`_max` — since
     * `UmbSorterController` only ever calls `onChange` from a real native drag, which happy-dom
     * can't simulate (it needs `DataTransfer` and real element layout geometry). CMS's own
     * `sorter.controller.test.ts` tests the controller the same way, via a non-private field on
     * its test scaffold.
     */
    _sorter = new UmbSorterController<UaiKeyValueListRow, HTMLElement>(this, {
        getUniqueOfElement: (element) => element.getAttribute("data-sort-entry-id"),
        getUniqueOfModel: (row) => row.id,
        identifier: "Uai.SorterIdentifier.KeyValueList",
        itemSelector: ".row",
        containerSelector: "#sorter-wrapper",
        onChange: ({ model }) => {
            this._rows = model;
            this.dispatchEvent(new UmbChangeEvent());
        },
    });

    @property({ type: Boolean, reflect: true })
    public set readonly(value: boolean) {
        this.#readonly = value;
        if (value) {
            this._sorter.disable();
        } else {
            this._sorter.enable();
        }
    }
    public get readonly(): boolean {
        return this.#readonly;
    }
    #readonly = false;

    override set value(items: UaiKeyValueListItem[] | undefined) {
        this.#setValue(items ?? []);
    }
    override get value(): UaiKeyValueListItem[] | undefined {
        if (this._rows.length === 0) return undefined;
        return this._rows.map(({ key, value }) => ({ key, value }));
    }

    @state()
    private _rows: UaiKeyValueListRow[] = [];

    @state()
    private _keyLabel?: string;

    @state()
    private _valueLabel?: string;

    @state()
    private _keyPlaceholder?: string;

    @state()
    private _valuePlaceholder?: string;

    @state()
    private _min?: number;

    @state()
    private _max?: number;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;

        this._keyLabel = config.getValueByAlias<string>("keyLabel");
        this._valueLabel = config.getValueByAlias<string>("valueLabel");
        this._keyPlaceholder = config.getValueByAlias<string>("keyPlaceholder");
        this._valuePlaceholder = config.getValueByAlias<string>("valuePlaceholder");
        this._min = config.getValueByAlias<number>("min");
        this._max = config.getValueByAlias<number>("max");
    }

    constructor() {
        super();

        this.addValidator(
            "rangeUnderflow",
            () => this.localize.term("uaiKeyValueList_minMessage", this._min ?? 0),
            () => !!this._min && this._rows.length < this._min,
        );
        this.addValidator(
            "rangeOverflow",
            () => this.localize.term("uaiKeyValueList_maxMessage", this._max ?? 0),
            () => !!this._max && this._rows.length > this._max,
        );
    }

    /**
     * The single choke point for replacing `_rows` — every mutation (set value, add, remove,
     * update) must go through here so the sorter's own model never drifts out of sync with what's
     * rendered. Mirrors `MultipleTextString`'s `items` setter, which is the one place that writes
     * `_items` and calls `#sorter.setModel(...)` together. Skipping this (e.g. assigning `_rows`
     * directly) is exactly the bug this fixes: the sorter builds its reorder result from its own
     * stored model, so a stale model silently reverts any edit made since the last sync as soon as
     * the user drags a row.
     *
     * The sorter's own `onChange` (below) writes `_rows` directly rather than routing back through
     * here — that's safe, not a second competing choke point, because by the time `onChange` fires
     * the sorter has already stored the reordered array as its own model, so `_rows` and the
     * sorter's model are already in sync and there's nothing left here to re-set.
     */
    #setRows(rows: UaiKeyValueListRow[]) {
        this._rows = rows;
        this._sorter.setModel(rows);
    }

    /**
     * Only rebuilds rows (and their ids) when the incoming value actually differs from ours.
     * Safe to short-circuit on "unchanged" because `_rows` and the sorter's model are only ever
     * written together via `#setRows` — if nothing changed, both are already in sync from
     * whatever mutation last ran, so there's nothing here to resync.
     */
    #setValue(items: UaiKeyValueListItem[]) {
        const current = this.value ?? [];
        const hasChanged =
            current.length !== items.length ||
            items.some((item, index) => item.key !== current[index]?.key || item.value !== current[index]?.value);

        if (!hasChanged) return;

        this.#setRows(items.map((item) => ({ id: crypto.randomUUID(), ...item })));
    }

    #onAdd() {
        this.#setRows([...this._rows, { id: crypto.randomUUID(), key: "", value: "" }]);
        this.dispatchEvent(new UmbChangeEvent());
        this.#focusLastRow();
    }

    async #focusLastRow() {
        await this.updateComplete;
        const inputs = this.shadowRoot?.querySelectorAll<UUIInputElement>(".row:last-of-type .key-input");
        inputs?.[inputs.length - 1]?.focus();
    }

    #onRemove(rowId: string) {
        this.#setRows(this._rows.filter((row) => row.id !== rowId));
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onKeyInput(event: InputEvent, rowId: string) {
        const target = event.target as UUIInputElement;
        this.#updateRow(rowId, { key: String(target.value ?? "") });
    }

    #onValueInput(event: InputEvent, rowId: string) {
        const target = event.target as UUIInputElement;
        this.#updateRow(rowId, { value: String(target.value ?? "") });
    }

    #updateRow(rowId: string, patch: Partial<UaiKeyValueListItem>) {
        this.#setRows(this._rows.map((row) => (row.id === rowId ? { ...row, ...patch } : row)));
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html` <div id="sorter-wrapper">${this.#renderRows()}</div>
            ${this.#renderAddButton()}`;
    }

    #renderRows() {
        return html`
            ${repeat(
                this._rows,
                (row) => row.id,
                (row) => html`
                    <div class="row" data-sort-entry-id=${row.id}>
                        ${when(
                            !this.readonly,
                            () => html`<uui-icon class="drag-handle" name="icon-navigation"></uui-icon>`,
                        )}
                        <uui-input
                            class="key-input"
                            label=${this._keyLabel ?? this.localize.term("uaiKeyValueList_keyLabel")}
                            placeholder=${this._keyPlaceholder ?? ""}
                            .value=${row.key}
                            ?readonly=${this.readonly}
                            @input=${(e: InputEvent) => this.#onKeyInput(e, row.id)}
                        ></uui-input>
                        <uui-input
                            class="value-input"
                            label=${this._valueLabel ?? this.localize.term("uaiKeyValueList_valueLabel")}
                            placeholder=${this._valuePlaceholder ?? ""}
                            .value=${row.value}
                            ?readonly=${this.readonly}
                            @input=${(e: InputEvent) => this.#onValueInput(e, row.id)}
                        ></uui-input>
                        ${when(
                            !this.readonly,
                            () => html`
                                <uui-button
                                    label=${this.localize.term("uaiKeyValueList_removeRow")}
                                    @click=${() => this.#onRemove(row.id)}
                                >
                                    <uui-icon name="icon-trash"></uui-icon>
                                </uui-button>
                            `,
                        )}
                    </div>
                `,
            )}
        `;
    }

    #renderAddButton() {
        if (this.readonly) return nothing;
        if (this._max && this._rows.length >= this._max) return nothing;

        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                label=${this.localize.term("uaiKeyValueList_addRow")}
                @click=${this.#onAdd}
            >
                <uui-icon name="icon-add"></uui-icon>
                ${this.localize.term("general_add")}
            </uui-button>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            #sorter-wrapper {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-2);
                margin-bottom: var(--uui-size-space-3);
            }

            .row {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
            }

            .row uui-input {
                flex: 1 1 0;
            }

            .drag-handle {
                cursor: grab;
                color: var(--uui-color-border-emphasis);
            }

            .drag-handle:active {
                cursor: grabbing;
            }

            #btn-add {
                display: block;
                width: 100%;
            }

            .--umb-sorter-placeholder {
                position: relative;
                visibility: hidden;
            }
            .--umb-sorter-placeholder::after {
                content: "";
                position: absolute;
                inset: 0px;
                border-radius: var(--uui-border-radius);
                border: 1px dashed var(--uui-color-divider-emphasis);
            }
        `,
    ];
}

export { UaiPropertyEditorUIKeyValueListElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIKeyValueListElement;
    }
}
