import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/sdk.gen.js";
import { UAI_ITEM_PICKER_MODAL, type UaiPickableItemModel } from "@umbraco-ai/core";
import type { UaiWorkflowItem } from "../../types.js";

const elementName = "uai-workflow-picker";

/**
 * Workflow picker component that allows selecting a registered workflow
 * for orchestrated agents. Single selection only.
 *
 * When a workflow is selected, its settings schema is available via
 * the `selectedWorkflow` property for rendering a settings editor.
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 *
 * @example
 * ```html
 * <uai-workflow-picker
 *   .value=${"write-and-edit"}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-workflow-picker>
 * ```
 * @public
 */
@customElement(elementName)
export class UaiWorkflowPickerElement extends UmbFormControlMixin<
    string | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    /**
     * Readonly mode - cannot add or remove.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * The selected workflow ID.
     */
    override set value(val: string | undefined) {
        this.#setValue(val);
    }
    override get value(): string | undefined {
        return this._selectedId ?? undefined;
    }

    /**
     * The full workflow item for the current selection, including settings schema.
     * Updated reactively when workflows load or selection changes.
     */
    @property({ attribute: false })
    public selectedWorkflow: UaiWorkflowItem | undefined;

    @state()
    private _selectedId: string | null = null;

    @state()
    private _allWorkflows: UaiWorkflowItem[] = [];

    @state()
    private _loading = false;

    @state()
    private _loaded = false;

    #setValue(val: string | undefined) {
        const newId = val || null;
        if (newId === this._selectedId) return;

        this._selectedId = newId;

        if (!newId) {
            this.selectedWorkflow = undefined;
            return;
        }

        // If workflows are already loaded, resolve immediately
        if (this._loaded) {
            this.selectedWorkflow = this._allWorkflows.find((w) => w.id === newId);
        } else {
            this.#loadWorkflows();
        }
    }

    async #loadWorkflows() {
        if (this._loading) return;
        this._loading = true;

        const { data } = await tryExecute(this, AgentsService.getAgentWorkflows());
        this._allWorkflows = (data as UaiWorkflowItem[] | undefined) ?? [];
        this._loaded = true;
        this._loading = false;

        // Resolve selected item after load
        if (this._selectedId) {
            this.selectedWorkflow = this._allWorkflows.find((w) => w.id === this._selectedId);
            this.dispatchEvent(new CustomEvent("workflow-loaded", { bubbles: true, composed: true }));
        }
    }

    async #openPicker() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#fetchAvailableWorkflows(),
                selectionMode: "single",
                title: "Select a workflow",
                noResultsMessage: "No workflows are registered. Workflows are code-based extension points that must be implemented in a .NET project.",
            },
        });

        try {
            const result = await modal.onSubmit();
            if (result?.selection?.length) {
                const selected = result.selection[0];
                this._selectedId = selected.value;
                this.selectedWorkflow = this._allWorkflows.find((w) => w.id === selected.value);
                this.dispatchEvent(new UmbChangeEvent());
            }
        } catch {
            // Modal was cancelled
        }
    }

    async #fetchAvailableWorkflows(): Promise<UaiPickableItemModel[]> {
        // Ensure workflows are loaded
        if (!this._loaded) {
            await this.#loadWorkflows();
        }

        return this._allWorkflows
            .filter((w) => w.id !== this._selectedId)
            .map((w) => ({
                value: w.id,
                label: w.name,
                description: w.description ?? undefined,
                icon: "icon-mindmap",
            }));
    }

    #onRemove() {
        this._selectedId = null;
        this.selectedWorkflow = undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`<div class="container">${this.#renderItem()} ${this.#renderAddButton()}</div>`;
    }

    #renderItem() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (!this.selectedWorkflow) return nothing;

        return html`
            <uui-ref-list>
                <uui-ref-node
                    name=${this.selectedWorkflow.name}
                    detail=${this.selectedWorkflow.description ?? ""}
                    readonly
                >
                    <umb-icon slot="icon" name="icon-mindmap"></umb-icon>
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
            </uui-ref-list>
        `;
    }

    #renderAddButton() {
        if (this.readonly) return nothing;
        if (this._selectedId) return nothing;

        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label="Select a workflow"
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

export default UaiWorkflowPickerElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiWorkflowPickerElement;
    }
}
