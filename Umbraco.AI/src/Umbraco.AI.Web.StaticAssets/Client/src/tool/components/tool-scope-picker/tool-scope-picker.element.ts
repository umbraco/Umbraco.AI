import { css, customElement, html, nothing, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { type ToolScopeItemResponseModel } from "../../repository/tool.repository.js";
import { UaiToolController } from "../../controllers/tool.controller.js";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { toCamelCase } from "../../utils.js";

const elementName = "uai-tool-scope-picker";

interface UaiToolScopeItemModel {
    id: string;
    name: string;
    description: string;
    domain: string;
    icon: string;
}

/**
 * Tool scope picker component that allows selecting one or more tool scopes.
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 *
 * @example
 * Multiple selection:
 * ```html
 * <uai-tool-scope-picker
 *   .value=${["content-read", "media-write"]}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-tool-scope-picker>
 * ```
 * @public
 */
@customElement(elementName)
export class UaiToolScopePickerElement extends UmbFormControlMixin<string[] | undefined, typeof UmbLitElement, undefined>(
    UmbLitElement,
    undefined,
) {
    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * Hide scopes that have no tools.
     */
    @property({ type: Boolean })
    hideEmptyScopes = false;

    /**
     * The selected tool scope ID(s).
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
    private _items: UaiToolScopeItemModel[] = [];

    @state()
    private _loading = false;

    @state()
    private _toolCounts: Record<string, number> = {};

    #toolController = new UaiToolController(this);

    override async connectedCallback() {
        super.connectedCallback();
        this._toolCounts = await this.#toolController.getToolCountsByScope();
    }

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

    async #loadItems() {
        if (this._selection.length === 0) {
            this._items = [];
            return;
        }

        this._loading = true;

        const { data, error } = await this.#toolController.getToolScopes();

        if (!error && data) {
            // Preserve selection order
            this._items = this._selection
                .map((id) => {
                    const scope = data.find((s: ToolScopeItemResponseModel) => s.id.toLowerCase() === id.toLowerCase());
                    if (!scope) return undefined;

                    const camelCaseId = toCamelCase(scope.id);
                    const localizedName = this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id;
                    const localizedDescription =
                        this.localize.term(`uaiToolScope_${camelCaseId}Description`) || "";
                    const toolCount = this._toolCounts[scope.id] ?? 0;

                    return {
                        id: scope.id,
                        name: `${localizedName} (${toolCount})`,
                        description: localizedDescription,
                        domain: scope.domain || "General",
                        icon: scope.icon || "icon-wand",
                    };
                })
                .filter((item): item is UaiToolScopeItemModel => item !== undefined);
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableScopes(),
                selectionMode: "multiple",
                title: this.localize.term("uaiToolScope_selectScope") || "Select Tool Scopes",
                noResultsMessage: this.localize.term("uaiAgent_noToolScopesAvailable") || "No tool scopes available",
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
        const { data } = await this.#toolController.getToolScopes();

        if (!data) return [];

        // Filter out already selected items and map to picker format
        return data
            .filter((scope: ToolScopeItemResponseModel) =>
                !this._selection.some((id) => id.toLowerCase() === scope.id.toLowerCase())
            )
            .filter((scope: ToolScopeItemResponseModel) =>
                !this.hideEmptyScopes || (this._toolCounts[scope.id] ?? 0) > 0
            )
            .map((scope: ToolScopeItemResponseModel) => {
                const camelCaseId = toCamelCase(scope.id);
                const localizedName = this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id;
                const localizedDescription =
                    this.localize.term(`uaiToolScope_${camelCaseId}Description`) || "";
                const toolCount = this._toolCounts[scope.id] ?? 0;

                return {
                    value: scope.id,
                    label: `${localizedName} (${toolCount})`,
                    description: localizedDescription,
                    icon: scope.icon || "icon-wand",
                };
            });
    }

    #addSelections(items: UaiPickableItemModel[]) {
        // Filter out already selected items
        const newValues = items
            .map((item) => item.value)
            .filter((value) => !this._selection.some((id) => id.toLowerCase() === value.toLowerCase()));

        if (newValues.length === 0) return;

        this._selection = [...this._selection, ...newValues];

        this.#loadItems();
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onRemove(id: string) {
        this._selection = this._selection.filter((x) => x.toLowerCase() !== id.toLowerCase());
        this._items = this._items.filter((x) => x.id.toLowerCase() !== id.toLowerCase());
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html` <div class="container">${this.#renderItems()} ${this.#renderAddButton()}</div> `;
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

    #renderItem(item: UaiToolScopeItemModel) {
        return html`
            <uui-ref-node name=${item.name} detail=${item.description} readonly>
                <umb-icon slot="icon" name=${item.icon}></umb-icon>
                <uui-tag slot="tag" look="secondary">${item.domain}</uui-tag>
                ${!this.readonly
                    ? html`
                          <uui-action-bar slot="actions">
                              <uui-button
                                  label="Remove"
                                  @click=${(e: Event) => {
                                      e.stopPropagation();
                                      this.#onRemove(item.id);
                                  }}
                              >
                                  <uui-icon name="icon-trash"></uui-icon>
                              </uui-button>
                          </uui-action-bar>
                      `
                    : nothing}
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
                label=${this.localize.term("uaiAgent_addScope") || "Add Scope"}
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

export default UaiToolScopePickerElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiToolScopePickerElement;
    }
}
