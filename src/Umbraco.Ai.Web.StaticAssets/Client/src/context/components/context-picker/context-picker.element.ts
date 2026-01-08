import {
    css,
    customElement,
    html,
    nothing,
    property,
    repeat,
    state,
    when,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { ContextsService } from '../../../api/sdk.gen.js';
import { UAI_ITEM_PICKER_MODAL } from '../../../core/modals/item-picker/item-picker-modal.token.js';
import type { UaiPickableItemModel } from '../../../core/modals/item-picker/types.js';
import { UaiContextTypeMapper } from '../../type-mapper.js';
import type { UaiContextItemModel } from '../../types.js';

const elementName = 'uai-context-picker';

@customElement(elementName)
export class UaiContextPickerElement extends UmbFormControlMixin<
    string[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    /**
     * Allow selecting multiple contexts.
     */
    @property({ type: Boolean })
    public multiple = false;

    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * Minimum number of required contexts.
     */
    @property({ type: Number })
    public min?: number;

    /**
     * Maximum number of allowed contexts.
     */
    @property({ type: Number })
    public max?: number;

    /**
     * The selected context IDs as JSON array.
     */
    override set value(val: string[] | undefined) {
        this.#setValue(val);
    }
    override get value(): string[] | undefined {
        return this._selection.length > 0 ? this._selection : undefined;
    }

    @state()
    private _selection: string[] = [];

    @state()
    private _items: UaiContextItemModel[] = [];

    @state()
    private _loading = false;

    #setValue(val: string[] | undefined) {
        if (!val) {
            this._selection = [];
            this._items = [];
            return;
        }
        this._selection = val;
        this.#loadItems();
    }

    async #loadItems() {
        if (this._selection.length === 0) {
            this._items = [];
            return;
        }

        this._loading = true;

        // Fetch all contexts and filter to selected ones
        const { data, error } = await tryExecute(
            this,
            ContextsService.getAllContexts({ query: { take: 1000 } })
        );

        if (!error && data) {
            const allItems = data.items.map(UaiContextTypeMapper.toItemModel);
            // Preserve selection order
            this._items = this._selection
                .map(id => allItems.find(item => item.unique === id))
                .filter((item): item is UaiContextItemModel => item !== undefined);
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableContexts(),
                selectionMode: this.multiple ? 'multiple' : 'single',
                title: this.localize.term('uaiContext_selectContext'),
                noResultsMessage: this.localize.term('uaiContext_noContextsAvailable'),
            },
        });

        try {
            const result = await modal.onSubmit();
            if (result?.selection?.length) {
                this.#addSelections(result.selection);
            }
        } catch {
            // Modal was cancelled
        }
    }

    async #fetchAvailableContexts(): Promise<UaiPickableItemModel[]> {
        const { data } = await tryExecute(
            this,
            ContextsService.getAllContexts({ query: { take: 1000 } })
        );

        if (!data) return [];

        // Filter out already selected items
        return data.items
            .filter(ctx => !this._selection.includes(ctx.id))
            .map(ctx => ({
                value: ctx.id,
                label: ctx.name,
                description: ctx.alias,
                icon: 'icon-wand',
            }));
    }

    #addSelections(items: UaiPickableItemModel[]) {
        // Filter out already selected items
        const newValues = items
            .map(item => item.value)
            .filter(value => !this._selection.includes(value));

        if (newValues.length === 0) return;

        if (this.multiple) {
            this._selection = [...this._selection, ...newValues];
        } else {
            // Single mode: only take the first item
            this._selection = [newValues[0]];
        }

        this.#loadItems();
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onRemove(id: string) {
        this._selection = this._selection.filter(x => x !== id);
        this._items = this._items.filter(x => x.unique !== id);
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <div class="container">
                ${this.#renderItems()}
                ${this.#renderAddButton()}
            </div>
        `;
    }

    #renderItems() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (!this._items.length) return nothing;

        return html`
            <uui-ref-list>
                ${repeat(
                    this._items,
                    (item) => item.unique,
                    (item) => this.#renderItem(item),
                )}
            </uui-ref-list>
        `;
    }

    #renderItem(item: UaiContextItemModel) {
        return html`
            <uui-ref-node
                name=${item.name}
                detail=${item.alias}
                readonly>
                <umb-icon slot="icon" name="icon-wand"></umb-icon>
                ${when(item.resourceCount > 0, () => html`
                    <uui-tag slot="tag" color="default">${item.resourceCount} resources</uui-tag>
                `)}
                ${when(!this.readonly, () => html`
                    <uui-action-bar slot="actions">
                        <uui-button
                            label="Remove"
                            @click=${(e: Event) => {
                                e.stopPropagation();
                                this.#onRemove(item.unique);
                            }}>
                            <uui-icon name="icon-trash"></uui-icon>
                        </uui-button>
                    </uui-action-bar>
                `)}
            </uui-ref-node>
        `;
    }

    #renderAddButton() {
        if (this.readonly) return nothing;
        if (this.max && this._selection.length >= this.max) return nothing;
        // For single-select, hide add button if already have a selection
        if (!this.multiple && this._selection.length > 0) return nothing;

        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label=${this.localize.term('uaiContext_addContext')}>
                <uui-icon name="icon-add"></uui-icon>
                ${this.localize.term('general_add')}
            </uui-button>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            .container {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
            }

            #btn-add {
                width: 100%;
            }

            uui-ref-list {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-1);
            }

            uui-ref-node {
                padding: var(--uui-size-space-3);
            }

            uui-ref-node::before {
                border-radius: var(--uui-border-radius);
                border: 1px solid var(--uui-color-divider-standalone);
            }
        `,
    ];
}

export { UaiContextPickerElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiContextPickerElement;
    }
}
