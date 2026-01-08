import {
    css,
    customElement,
    html,
    nothing,
    property,
    repeat,
    state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import type { UaiContextResourceModel } from '../../types.js';
import type { UaiContextResourceTypeItemModel } from '../../../context-resource-type/types.js';
import { UaiContextResourceTypeItemRepository } from '../../../context-resource-type/repository/item/context-resource-type-item.repository.js';
import { UAI_CONTEXT_RESOURCE_TYPE_PICKER_MODAL } from './context-resource-type-picker-modal.token.js';
import { UAI_RESOURCE_OPTIONS_MODAL } from './resource-options-modal.token.js';

const elementName = 'uai-resource-list';

export interface UaiResourceCardModel extends UaiContextResourceModel {
    resourceType?: UaiContextResourceTypeItemModel;
}

@customElement(elementName)
export class UaiResourceListElement extends UmbLitElement {
    #contextResourceTypeItemRepository = new UaiContextResourceTypeItemRepository(this);
    #contextResourceTypes: UaiContextResourceTypeItemModel[] = [];

    @property({ type: Array })
    public get items(): UaiContextResourceModel[] {
        return this._items;
    }
    public set items(value: UaiContextResourceModel[]) {
        this._items = value ?? [];
        this.#updateCards();
    }

    @state()
    private _items: UaiContextResourceModel[] = [];

    @state()
    private _cards: UaiResourceCardModel[] = [];

    @property({ type: Boolean, reflect: true })
    public readonly = false;

    constructor() {
        super();
        this.#loadContextResourceTypes();
    }

    async #loadContextResourceTypes() {
        const { data } = await this.#contextResourceTypeItemRepository.requestItems();
        if (data) {
            this.#contextResourceTypes = data;
            this.#updateCards();
        }
    }

    #updateCards() {
        this._cards = this._items.map(item => ({
            ...item,
            resourceType: this.#contextResourceTypes.find(rt => rt.id === item.resourceTypeId),
        }));
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const pickerModal = modalManager.open(this, UAI_CONTEXT_RESOURCE_TYPE_PICKER_MODAL, {
            data: {
                contextResourceTypes: this.#contextResourceTypes,
            },
        });

        const result = await pickerModal.onSubmit();
        if (!result?.contextResourceType || !result?.resource) return;

        // Create the new resource
        const newResource: UaiContextResourceModel = {
            id: crypto.randomUUID(),
            resourceTypeId: result.contextResourceType.id,
            name: result.resource.name,
            description: result.resource.description ?? null,
            sortOrder: this._items.length,
            data: result.resource.data,
            injectionMode: result.resource.injectionMode,
        };

        this._items = [...this._items, newResource];
        this.#updateCards();
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #onEdit(card: UaiResourceCardModel) {
        if (this.readonly) return;

        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const optionsModal = modalManager.open(this, UAI_RESOURCE_OPTIONS_MODAL, {
            data: {
                resourceType: card.resourceType,
                resource: {
                    name: card.name,
                    description: card.description,
                    data: card.data ?? {},
                    injectionMode: card.injectionMode,
                },
            },
        });

        try {
            const optionsResult = await optionsModal.onSubmit();
            if (!optionsResult?.resource) return;

            // Update the existing resource
            this._items = this._items.map(item =>
                item.id === card.id
                    ? {
                        ...item,
                        name: optionsResult.resource.name,
                        description: optionsResult.resource.description ?? null,
                        data: optionsResult.resource.data,
                        injectionMode: optionsResult.resource.injectionMode,
                    }
                    : item
            );
            this.#updateCards();
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // Modal was cancelled - do nothing
        }
    }

    #onRemove(card: UaiResourceCardModel) {
        this._items = this._items.filter(x => x.id !== card.id);
        this.#updateCards();
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`<div class="container">${this.#renderItems()} ${this.#renderAddButton()}</div>`;
    }

    #renderItems() {
        if (!this._cards?.length) return nothing;
        return html`
            ${repeat(
                this._cards,
                (item) => item.id,
                (item) => this.#renderItem(item),
            )}
        `;
    }

    #renderAddButton() {
        if (this.readonly) return nothing;
        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label="Add resource">
                <uui-icon name="icon-add"></uui-icon>
                Add
            </uui-button>
        `;
    }

    #renderItem(card: UaiResourceCardModel) {
        const injectionLabel = card.injectionMode === 'Always' ? 'Always' : 'On-Demand';
        return html`
            <uui-card-block-type
                name=${card.name}
                description=${injectionLabel}
                @open=${() => this.#onEdit(card)}
                ?readonly=${this.readonly}>
                <umb-icon name=${card.resourceType?.icon ?? 'icon-document'}></umb-icon>
                <uui-action-bar slot="actions">
                    ${this.#renderRemoveAction(card)}
                </uui-action-bar>
            </uui-card-block-type>
        `;
    }

    #renderRemoveAction(card: UaiResourceCardModel) {
        if (this.readonly) return nothing;
        return html`
            <uui-button
                label="Remove"
                look="secondary"
                @click=${(e: Event) => {
                    e.stopPropagation();
                    this.#onRemove(card);
                }}>
                <uui-icon name="icon-trash"></uui-icon>
            </uui-button>
        `;
    }

    static override styles = [
        css`
            :host {
                position: relative;
            }
            .container {
                display: grid;
                gap: var(--uui-size-space-3);
                grid-template-columns: repeat(auto-fill, minmax(var(--umb-card-medium-min-width), 1fr));
                grid-auto-rows: var(--umb-card-medium-min-width);
            }

            #btn-add {
                text-align: center;
                height: 100%;
            }

            uui-card-block-type {
                cursor: pointer;
                min-width: auto;
            }

            uui-card-block-type[readonly] {
                cursor: default;
            }

            uui-card-block-type:hover:not([readonly]) {
                background-color: var(--uui-color-surface-emphasis);
            }
        `,
    ];
}

export { UaiResourceListElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiResourceListElement;
    }
}
