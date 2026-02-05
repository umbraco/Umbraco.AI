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
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import { AgentsService } from '../../../api/sdk.gen.js';
import { UAI_ITEM_PICKER_MODAL, type UaiPickableItemModel } from '@umbraco-ai/core';

const elementName = 'uai-scope-picker';

interface UaiScopeItemModel {
    id: string;
    icon: string;
    name: string;
    description: string;
}

@customElement(elementName)
export class UaiScopePickerElement extends UmbFormControlMixin<
    string | string[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    /**
     * Allow selecting multiple scopes.
     */
    @property({ type: Boolean })
    public multiple = false;

    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * The selected scope ID(s).
     * - Single mode: string | undefined
     * - Multiple mode: string[] | undefined
     */
    override set value(val: string | string[] | undefined) {
        this.#setValue(val);
    }
    override get value(): string | string[] | undefined {
        if (this._selection.length === 0) return undefined;
        return this.multiple ? this._selection : this._selection[0];
    }

    @state()
    private _selection: string[] = [];

    @state()
    private _items: UaiScopeItemModel[] = [];

    @state()
    private _loading = false;

    #setValue(val: string | string[] | undefined) {
        // Normalize to array for comparison
        const newSelection = !val ? [] : (Array.isArray(val) ? val : [val]);

        // Check if selection actually changed
        const hasChanged =
            newSelection.length !== this._selection.length ||
            newSelection.some((id, index) => id !== this._selection[index]);

        if (!hasChanged) {
            // Value hasn't changed, skip update
            return;
        }

        this._selection = newSelection;

        if (newSelection.length === 0) {
            this._items = [];
            return;
        }

        this.#loadItems();
    }

    async #loadItems() {
        if (this._selection.length === 0) {
            this._items = [];
            return;
        }

        this._loading = true;

        // Fetch all scopes and filter to selected ones
        const { data, error } = await tryExecute(
            this,
            AgentsService.getAgentScopes()
        );

        if (!error && data) {
            // Map to internal model with localized name/description
            this._items = this._selection
                .map(id => {
                    const scope = data.find(s => s.id === id);
                    if (!scope) return undefined;
                    return {
                        id: scope.id,
                        icon: scope.icon,
                        name: this.localize.term(`uaiAgentScope_${scope.id}Label`) || scope.id,
                        description: this.localize.term(`uaiAgentScope_${scope.id}Description`) || '',
                    };
                })
                .filter((item): item is UaiScopeItemModel => item !== undefined);
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableScopes(),
                selectionMode: this.multiple ? 'multiple' : 'single',
                title: this.localize.term('uaiAgent_selectScope'),
                noResultsMessage: this.localize.term('uaiAgent_noScopesAvailable'),
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

    async #fetchAvailableScopes(): Promise<UaiPickableItemModel[]> {
        const { data } = await tryExecute(
            this,
            AgentsService.getAgentScopes()
        );

        if (!data) return [];

        // Filter out already selected items
        return data
            .filter(scope => !this._selection.includes(scope.id))
            .map(scope => ({
                value: scope.id,
                label: this.localize.term(`uaiAgentScope_${scope.id}Label`) || scope.id,
                description: this.localize.term(`uaiAgentScope_${scope.id}Description`) || '',
                icon: scope.icon,
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
        this._items = this._items.filter(x => x.id !== id);
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

    #renderItem(item: UaiScopeItemModel) {
        return html`
            <uui-ref-node
                name=${item.name}
                detail=${item.description}
                readonly>
                <umb-icon slot="icon" name=${item.icon}></umb-icon>
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
        // For single-select, hide add button if already have a selection
        if (!this.multiple && this._selection.length > 0) return nothing;

        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label=${this.localize.term('uaiAgent_addScope')}>
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

export default UaiScopePickerElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiScopePickerElement;
    }
}
