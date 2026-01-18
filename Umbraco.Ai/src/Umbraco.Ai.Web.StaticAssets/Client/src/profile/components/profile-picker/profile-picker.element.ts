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
import { ProfilesService } from '../../../api/sdk.gen.js';
import { UAI_ITEM_PICKER_MODAL } from '../../../core/modals/item-picker/item-picker-modal.token.js';
import type { UaiPickableItemModel } from '../../../core/modals/item-picker/types.js';
import { UaiProfileTypeMapper } from '../../type-mapper.js';
import type { UaiProfileItemModel } from '../../types.js';

const elementName = 'uai-profile-picker';

/**
 * Profile picker component that allows selecting one or more AI profiles.
 * Can be filtered by capability (e.g., "Chat", "Embedding").
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 *
 * @example
 * Single selection (default):
 * ```html
 * <uai-profile-picker
 *   capability="Chat"
 *   .value=${"profile-id"}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-profile-picker>
 * ```
 * Multiple selection:
 * ```html
 * <uai-profile-picker
 *   multiple
 *   .value=${["profile-id-1", "profile-id-2"]}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-profile-picker>
 * ```
 * @public
 */
@customElement(elementName)
export class UaiProfilePickerElement extends UmbFormControlMixin<
    string | string[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    /**
     * Filter profiles by capability. If not set, all profiles are shown.
     */
    @property({ type: String })
    public capability?: string;

    /**
     * Allow selecting multiple profiles.
     */
    @property({ type: Boolean })
    public multiple = false;

    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * Minimum number of required profiles.
     */
    @property({ type: Number })
    public min?: number;

    /**
     * Maximum number of allowed profiles.
     */
    @property({ type: Number })
    public max?: number;

    /**
     * The selected profile ID(s).
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
    private _items: UaiProfileItemModel[] = [];

    @state()
    private _loading = false;

    #setValue(val: string | string[] | undefined) {
        if (!val) {
            this._selection = [];
            this._items = [];
            return;
        }
        // Normalize to array internally
        this._selection = Array.isArray(val) ? val : [val];
        this.#loadItems();
    }

    async #loadItems() {
        if (this._selection.length === 0) {
            this._items = [];
            return;
        }

        this._loading = true;

        // Fetch all profiles and filter to selected ones
        const { data, error } = await tryExecute(
            this,
            ProfilesService.getAllProfiles({ query: { take: 1000 } })
        );

        if (!error && data) {
            const allItems = data.items.map(UaiProfileTypeMapper.toItemModel);
            // Preserve selection order
            this._items = this._selection
                .map(id => allItems.find(item => item.unique === id))
                .filter((item): item is UaiProfileItemModel => item !== undefined);
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableProfiles(),
                selectionMode: this.multiple ? 'multiple' : 'single',
                title: this.localize.term('uaiProfile_selectProfile'),
                noResultsMessage: this.localize.term('uaiProfile_noProfilesAvailable'),
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

    async #fetchAvailableProfiles(): Promise<UaiPickableItemModel[]> {
        const { data } = await tryExecute(
            this,
            ProfilesService.getAllProfiles({ query: { take: 1000 } })
        );

        if (!data) return [];

        // Filter by capability if specified
        let profiles = data.items;
        if (this.capability) {
            profiles = profiles.filter(
                (p) => p.capability.toLowerCase() === this.capability!.toLowerCase()
            );
        }

        // Filter out already selected items
        return profiles
            .filter(profile => !this._selection.includes(profile.id))
            .map(profile => ({
                value: profile.id,
                label: profile.name,
                description: profile.alias,
                icon: this.#getCapabilityIcon(profile.capability),
            }));
    }

    #getCapabilityIcon(capability: string): string {
        const capabilityLower = capability.toLowerCase();
        if (capabilityLower === 'chat') return 'icon-chat';
        if (capabilityLower === 'embedding') return 'icon-list';
        return 'icon-wand';
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

    #renderItem(item: UaiProfileItemModel) {
        return html`
            <uui-ref-node
                name=${item.name}
                detail=${item.alias}
                readonly>
                <umb-icon slot="icon" name=${this.#getCapabilityIcon(item.capability)}></umb-icon>
                ${when(item.model, () => html`
                    <uui-tag slot="tag" look="secondary">${item.model!.modelId}</uui-tag>
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
                label=${this.localize.term('uaiProfile_addProfile')}>
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

export default UaiProfilePickerElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-picker": UaiProfilePickerElement;
    }
}
