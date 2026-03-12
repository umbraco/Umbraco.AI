import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UAI_ITEM_PICKER_MODAL } from "@umbraco-ai/core";
import type { UaiPickableItemModel } from "@umbraco-ai/core";
import { AgentsService } from "../../../api/index.js";

const elementName = "uai-agent-picker";

interface UaiAgentItemModel {
    id: string;
    name: string;
    description: string;
}

/**
 * Agent picker component that allows selecting a single AI agent.
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 * @public
 */
@customElement(elementName)
export class UaiAgentPickerElement extends UmbFormControlMixin<string | undefined, typeof UmbLitElement, undefined>(
    UmbLitElement,
    undefined,
) {
    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    override set value(val: string | undefined) {
        this.#setValue(val);
    }
    override get value(): string | undefined {
        return this._selectedId;
    }

    /** The display name of the currently selected agent. */
    get selectedName(): string | undefined {
        return this._item?.name;
    }

    @state()
    private _selectedId?: string;

    @state()
    private _item?: UaiAgentItemModel;

    @state()
    private _loading = false;

    #setValue(val: string | undefined) {
        if (val === this._selectedId) return;

        this._selectedId = val;

        if (!val) {
            this._item = undefined;
            return;
        }

        this.#loadItem();
    }

    async #loadItem() {
        if (!this._selectedId) {
            this._item = undefined;
            return;
        }

        this._loading = true;

        const { data, error } = await tryExecute(
            this,
            AgentsService.getAllAgents({ query: { skip: 0, take: 100 } }),
        );

        if (!error && data) {
            const agent = data.items.find(
                (a) => a.id?.toLowerCase() === this._selectedId?.toLowerCase(),
            );
            if (agent) {
                this._item = {
                    id: agent.id!,
                    name: agent.name,
                    description: agent.description ?? "",
                };
            }
        }

        this._loading = false;
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableAgents(),
                selectionMode: "single",
                title: "Select Agent",
            },
        });

        try {
            const result = await modal.onSubmit();
            if (result?.selection?.length) {
                const selected = result.selection[0];
                this._selectedId = selected.value;
                this._item = {
                    id: selected.value,
                    name: selected.label,
                    description: selected.description ?? "",
                };
                this.dispatchEvent(new UmbChangeEvent());
            }
        } catch {
            // Modal was cancelled
        }
    }

    async #fetchAvailableAgents(): Promise<UaiPickableItemModel[]> {
        const { data } = await tryExecute(
            this,
            AgentsService.getAllAgents({ query: { skip: 0, take: 100 } }),
        );

        if (!data) return [];

        return data.items
            .filter((agent) => agent.id?.toLowerCase() !== this._selectedId?.toLowerCase())
            .map((agent) => ({
                value: agent.id!,
                label: agent.name,
                description: agent.description ?? undefined,
                icon: "icon-bot",
            }));
    }

    #onRemove() {
        this._selectedId = undefined;
        this._item = undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`<div class="container">${this.#renderItem()} ${this.#renderAddButton()}</div>`;
    }

    #renderItem() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (!this._item) return nothing;

        return html`
            <uui-ref-node name=${this._item.name} detail=${this._item.description} readonly>
                <umb-icon slot="icon" name="icon-bot"></umb-icon>
                ${!this.readonly
                    ? html`
                          <uui-action-bar slot="actions">
                              <uui-button
                                  label="Remove"
                                  @click=${(e: Event) => {
                                      e.stopPropagation();
                                      this.#onRemove();
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
        if (this.readonly || this._item) return nothing;

        return html`
            <uui-button id="btn-add" look="placeholder" @click=${this.#openPicker} label="Select Agent">
                <uui-icon name="icon-add"></uui-icon>
                Add
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

export default UaiAgentPickerElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiAgentPickerElement;
    }
}
