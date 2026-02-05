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
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { UaiToolRepository } from '../../repository/tool.repository.js';
import { UAI_ITEM_PICKER_MODAL } from '../../../core/modals/item-picker/item-picker-modal.token.js';
import type { UaiPickableItemModel } from '../../../core/modals/item-picker/types.js';

const elementName = 'uai-tool-picker';

interface UaiToolItemModel {
    id: string;
    name: string;
    description: string;
    scopeId: string;
    isDestructive: boolean;
}

/**
 * Tool picker component that allows selecting one or more AI tools.
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 *
 * @example
 * Multiple selection (default):
 * ```html
 * <uai-tool-picker
 *   .value=${["tool-id-1", "tool-id-2"]}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-tool-picker>
 * ```
 * @public
 */
@customElement(elementName)
export class UaiToolPickerElement extends UmbFormControlMixin<
    string[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * The selected tool ID(s).
     */
    override set value(val: string[] | undefined) {
        this.#setValue(val);
    }
    override get value(): string[] | undefined {
        if (this._selection.length === 0) return undefined;
        return this._selection;
    }

    @state()
    private _selection: string[] = [];

    @state()
    private _items: UaiToolItemModel[] = [];

    @state()
    private _loading = false;

    #toolRepository = new UaiToolRepository(this);

    #setValue(val: string[] | undefined) {
        const newSelection = val ?? [];

        // Check if selection actually changed
        const hasChanged =
            newSelection.length !== this._selection.length ||
            newSelection.some((id, index) => id !== this._selection[index]);

        if (!hasChanged) {
            return;
        }

        this._selection = newSelection;

        if (newSelection.length === 0) {
            this._items = [];
            return;
        }

        this.#loadItems();
    }

    #toCamelCase(str: string): string {
        return str
            .split(/[-_.\s]+/)
            .map((word, index) =>
                index === 0
                    ? word.toLowerCase()
                    : word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
            )
            .join('');
    }

    async #loadItems() {
        if (this._selection.length === 0) {
            this._items = [];
            return;
        }

        this._loading = true;

        const { data, error } = await this.#toolRepository.getTools();

        if (!error && data) {
            // Preserve selection order
            this._items = this._selection
                .map(id => {
                    const tool = data.find(t => t.id.toLowerCase() === id.toLowerCase());
                    if (!tool) return undefined;

                    const camelCaseId = this.#toCamelCase(tool.id);
                    const localizedName = this.localize.term(`uaiTool_${camelCaseId}Label`) || tool.name;
                    const localizedDescription = this.localize.term(`uaiTool_${camelCaseId}Description`) || tool.description;

                    return {
                        id: tool.id,
                        name: localizedName,
                        description: localizedDescription,
                        scopeId: tool.scopeId,
                        isDestructive: tool.isDestructive,
                    };
                })
                .filter((item): item is UaiToolItemModel => item !== undefined);
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableTools(),
                selectionMode: 'multiple',
                title: this.localize.term('uaiTool_selectTool'),
                noResultsMessage: this.localize.term('uaiTool_noToolsAvailable'),
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

    async #fetchAvailableTools(): Promise<UaiPickableItemModel[]> {
        const { data } = await this.#toolRepository.getTools();

        if (!data) return [];

        // Filter out already selected items
        return data
            .filter(tool => !this._selection.some(id => id.toLowerCase() === tool.id.toLowerCase()))
            .map(tool => {
                const camelCaseId = this.#toCamelCase(tool.id);
                const localizedName = this.localize.term(`uaiTool_${camelCaseId}Label`) || tool.name;
                const localizedDescription = this.localize.term(`uaiTool_${camelCaseId}Description`) || tool.description;

                return {
                    value: tool.id,
                    label: localizedName,
                    description: localizedDescription,
                    icon: tool.isDestructive ? 'icon-alert' : 'icon-wand',
                };
            });
    }

    #addSelections(items: UaiPickableItemModel[]) {
        // Filter out already selected items
        const newValues = items
            .map(item => item.value)
            .filter(value => !this._selection.some(id => id.toLowerCase() === value.toLowerCase()));

        if (newValues.length === 0) return;

        this._selection = [...this._selection, ...newValues];

        this.#loadItems();
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onRemove(id: string) {
        this._selection = this._selection.filter(x => x.toLowerCase() !== id.toLowerCase());
        this._items = this._items.filter(x => x.id.toLowerCase() !== id.toLowerCase());
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
                    (item) => item.id,
                    (item) => this.#renderItem(item),
                )}
            </uui-ref-list>
        `;
    }

    #renderItem(item: UaiToolItemModel) {
        return html`
            <uui-ref-node
                name=${item.name}
                detail=${item.description}
                readonly>
                <umb-icon slot="icon" name=${item.isDestructive ? 'icon-alert' : 'icon-wand'}></umb-icon>
                <uui-tag slot="tag" look="secondary">${item.scopeId}</uui-tag>
                ${!this.readonly ? html`
                    <uui-action-bar slot="actions">
                        <uui-button
                            label="Remove"
                            @click=${(e: Event) => {
                                e.stopPropagation();
                                this.#onRemove(item.id);
                            }}>
                            <uui-icon name="icon-trash"></uui-icon>
                        </uui-button>
                    </uui-action-bar>
                ` : nothing}
            </uui-ref-node>
        `;
    }

    #renderAddButton() {
        if (this.readonly) return nothing;

        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label=${this.localize.term('uaiTool_addTool')}>
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

export default UaiToolPickerElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiToolPickerElement;
    }
}
