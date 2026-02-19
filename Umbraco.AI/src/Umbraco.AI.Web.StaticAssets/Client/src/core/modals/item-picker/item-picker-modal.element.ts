import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { customElement, html, css, state, repeat, when, unsafeHTML } from "@umbraco-cms/backoffice/external/lit";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UUIMenuItemEvent } from "@umbraco-cms/backoffice/external/uui";
import type { UaiPickableItemModel } from "./types.js";
import { UaiSelectedEvent } from "../../events";
import type { UaiItemPickerModalData, UaiItemPickerModalValue } from "./item-picker-modal.token.js";

@customElement("uai-item-picker-modal")
export class UaiItemPickerModalElement extends UmbModalBaseElement<UaiItemPickerModalData, UaiItemPickerModalValue> {
    @state()
    private _loaded = false;

    @state()
    private _items: Array<UaiPickableItemModel> = [];

    @state()
    private _filteredItems: Array<UaiPickableItemModel> = [];

    @state()
    private _selectedItems: Array<UaiPickableItemModel> = [];

    async connectedCallback() {
        super.connectedCallback();
        if (this.data?.items) {
            this._items = this._filteredItems = [...this.data.items];
            this._loaded = true;
        } else if (this.data?.fetchItems) {
            const items = await this.data.fetchItems();
            this._items = this._filteredItems = [...items];
            this._loaded = true;
        } else {
            throw new Error("You must supply either an items or fetchItems data option");
        }
    }

    #filter(event: { target: HTMLInputElement }) {
        if (!this.data) return;

        this._selectedItems = [];
        this.updateValue({ selection: undefined });

        if (event.target.value) {
            const query = event.target.value.toLowerCase();
            this._filteredItems = this._items.filter(
                (item) => item.label.toLowerCase().includes(query) || item.value.toLowerCase().includes(query),
            );
        } else {
            this._filteredItems = this._items;
        }
    }

    #onClick(event: UUIMenuItemEvent, item: UaiPickableItemModel) {
        event.stopPropagation();
        if (this.data?.selectionMode === "multiple") {
            // Toggle selection for multiple mode
            const index = this._selectedItems.findIndex((s) => s.value === item.value);
            if (index === -1) {
                this._selectedItems = [...this._selectedItems, item];
            } else {
                this._selectedItems = this._selectedItems.filter((s) => s.value !== item.value);
            }
            this.updateValue({ selection: this._selectedItems });
        } else {
            // Single selection - dispatch event
            this.updateValue({ selection: [item] });
            this.modalContext?.dispatchEvent(new UaiSelectedEvent(item.value, item));

            // Auto-submit unless explicitly disabled
            if (this.data?.autoSubmit !== false) {
                this._submitModal();
            }
        }
    }

    #isSelected(item: UaiPickableItemModel): boolean {
        return this._selectedItems.some((s) => s.value === item.value);
    }

    #onConfirm() {
        // Dispatch event for each selected item
        for (const item of this._selectedItems) {
            this.modalContext?.dispatchEvent(new UaiSelectedEvent(item.value, item));
        }
        this._submitModal();
    }

    render() {
        if (!this.data) return;

        return html`
            <umb-body-layout
                headline=${this.data?.title
                    ? this.localize.string(this.data.title)
                    : this.localize.term("ucPlaceholders_selectAnItem")}
            >
                <div id="main">
                    <uui-input
                        label="Search"
                        type="search"
                        placeholder=${this.localize.term("placeholders_filter")}
                        @input=${this.#filter}
                    >
                        <div class="icon-holder" slot="prepend">
                            <uui-icon name="search"></uui-icon>
                        </div>
                    </uui-input>
                    <uui-box>
                        ${when(
                            this._filteredItems.length > 0,
                            () =>
                                html` <uui-ref-list>
                                    ${repeat(
                                        this._filteredItems,
                                        (item) => item.value,
                                        (item) => html`
                                            <uui-ref-node
                                                name=${item.label}
                                                detail=${item.description}
                                                ?select-only=${this.data?.selectionMode === "multiple"}
                                                ?selectable=${this.data?.selectionMode === "multiple"}
                                                ?selected=${this.#isSelected(item)}
                                                @open=${(e: UUIMenuItemEvent) => this.#onClick(e, item)}
                                                @selected=${(e: UUIMenuItemEvent) => this.#onClick(e, item)}
                                                @deselected=${(e: UUIMenuItemEvent) => this.#onClick(e, item)}
                                            >
                                                <umb-icon
                                                    slot="icon"
                                                    name=${item.icon!}
                                                    style="--uui-icon-color: ${item.color ?? "#000000"}"
                                                ></umb-icon>
                                                ${when(
                                                    this.data!.tagTemplate,
                                                    () =>
                                                        html`<div
                                                            slot="tag"
                                                            style="line-height: 1rem;text-align: right; padding-left: 10px; white-space: nowrap;"
                                                        >
                                                            ${this.data!.tagTemplate!(item)}
                                                        </div>`,
                                                )}
                                            </uui-ref-node>
                                        `,
                                    )}
                                </uui-ref-list>`,
                            () =>
                                when(
                                    this._loaded,
                                    () =>
                                        html`<p
                                            style="font-size: 14px; text-align: center; margin: var(--uui-size-space-6);"
                                        >
                                            ${unsafeHTML(
                                                this.localize.string(
                                                    this.data!.noResultsMessage ?? "#uaiMessage_noResults",
                                                ),
                                            )}
                                        </p>`,
                                ),
                        )}
                    </uui-box>
                </div>
                <uui-button
                    slot="actions"
                    id="close"
                    label=${this.localize.term("general_close")}
                    @click="${this._rejectModal}"
                ></uui-button>
                ${when(
                    this.data.selectionMode === "multiple",
                    () =>
                        html`<uui-button
                            slot="actions"
                            id="confirm"
                            color="positive"
                            look="primary"
                            label=${this.data?.buttonLabel
                                ? this.localize.string(this.data.buttonLabel)
                                : this.localize.term("uaiGeneral_select")}
                            @click=${this.#onConfirm}
                            ?disabled=${this._selectedItems.length === 0}
                            type="button"
                        ></uui-button>`,
                )}
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                position: relative;
            }
            #main {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }
            uui-ref-node {
                padding-top: calc(var(--uui-size-2, 6px) + 5px);
                padding-bottom: calc(var(--uui-size-2, 6px) + 5px);
            }
            //uui-ref-node:first-child{
            //    padding-top: 0;
            //}
            //uui-ref-node:last-child{
            //    padding-bottom: 0;
            //}
            uui-ref-node::before {
                border-top: 1px solid var(--uui-color-divider-standalone);
            }
            uui-input {
                width: 100%;
            }
            .icon-holder {
                display: flex;
                align-items: center;
            }
        `,
    ];
}

export default UaiItemPickerModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-item-picker-modal": UaiItemPickerModalElement;
    }
}
