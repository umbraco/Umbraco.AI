import { css, html, customElement, state, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiOrchestrationRouterNodeEditorModalData,
    UaiOrchestrationRouterNodeEditorModalValue,
} from "./router-node-editor-modal.token.js";
import type { UaiOrchestrationNode, UaiOrchestrationRouteCondition } from "../../types.js";

const OPERATORS = ["Equals", "Contains", "StartsWith", "Matches"];

/**
 * Modal for editing Router node configuration.
 * Provides a condition builder with field + operator + value rows.
 */
@customElement("uai-orchestration-router-node-editor-modal")
export class UaiOrchestrationRouterNodeEditorModalElement extends UmbModalBaseElement<
    UaiOrchestrationRouterNodeEditorModalData,
    UaiOrchestrationRouterNodeEditorModalValue
> {
    @state()
    private _node!: UaiOrchestrationNode;

    @state()
    private _conditions: UaiOrchestrationRouteCondition[] = [];

    connectedCallback() {
        super.connectedCallback();
        if (this.data?.node) {
            this._node = structuredClone(this.data.node);
            this._conditions = [...(this._node.config?.conditions ?? [])];
        }
    }

    #onLabelChange(event: Event) {
        this._node = { ...this._node, label: (event.target as HTMLInputElement).value };
    }

    #addCondition() {
        this._conditions = [
            ...this._conditions,
            { label: "", field: "", operator: "Equals", value: "", targetNodeId: "" },
        ];
    }

    #removeCondition(index: number) {
        this._conditions = this._conditions.filter((_, i) => i !== index);
    }

    #updateCondition(index: number, field: keyof UaiOrchestrationRouteCondition, value: string) {
        const updated = [...this._conditions];
        updated[index] = { ...updated[index], [field]: value };
        this._conditions = updated;
    }

    #onDelete() {
        this.value = { node: this._node, deleted: true };
        this.modalContext?.submit();
    }

    #onSubmit() {
        this._node = {
            ...this._node,
            config: { ...this._node.config, conditions: this._conditions },
        };
        this.value = { node: this._node };
        this.modalContext?.submit();
    }

    render() {
        if (!this._node) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-body-layout headline="Router Node">
                <div id="main">
                    <uui-box>
                        <umb-property-layout label="Label" description="Display name for this node">
                            <uui-input
                                slot="editor"
                                .value=${this._node.label}
                                @input=${this.#onLabelChange}
                                placeholder="Router"
                            ></uui-input>
                        </umb-property-layout>
                    </uui-box>

                    <uui-box headline="Conditions">
                        <div class="conditions-header">
                            <uui-button
                                compact
                                look="outline"
                                @click=${this.#addCondition}
                                label="Add Condition"
                            >
                                <uui-icon name="icon-add"></uui-icon>
                                Add
                            </uui-button>
                        </div>

                        ${this._conditions.length === 0
                            ? html`<p class="empty-state">
                                  No conditions. Add conditions to define routing rules, or leave
                                  empty for a default pass-through.
                              </p>`
                            : ""}

                        ${repeat(
                            this._conditions,
                            (_c, i) => i,
                            (condition, index) => html`
                                <div class="condition-row">
                                    <uui-input
                                        placeholder="Label"
                                        .value=${condition.label}
                                        @input=${(e: Event) =>
                                            this.#updateCondition(
                                                index,
                                                "label",
                                                (e.target as HTMLInputElement).value,
                                            )}
                                    ></uui-input>
                                    <uui-input
                                        placeholder="Field"
                                        .value=${condition.field}
                                        @input=${(e: Event) =>
                                            this.#updateCondition(
                                                index,
                                                "field",
                                                (e.target as HTMLInputElement).value,
                                            )}
                                    ></uui-input>
                                    <uui-select
                                        .options=${OPERATORS.map((op) => ({
                                            name: op,
                                            value: op,
                                            selected: op === condition.operator,
                                        }))}
                                        @change=${(e: Event) =>
                                            this.#updateCondition(
                                                index,
                                                "operator",
                                                (e.target as HTMLSelectElement).value,
                                            )}
                                    ></uui-select>
                                    <uui-input
                                        placeholder="Value"
                                        .value=${condition.value}
                                        @input=${(e: Event) =>
                                            this.#updateCondition(
                                                index,
                                                "value",
                                                (e.target as HTMLInputElement).value,
                                            )}
                                    ></uui-input>
                                    <uui-button
                                        compact
                                        color="danger"
                                        @click=${() => this.#removeCondition(index)}
                                        label="Remove"
                                    >
                                        <uui-icon name="icon-trash"></uui-icon>
                                    </uui-button>
                                </div>
                            `,
                        )}
                    </uui-box>
                </div>
                <div slot="actions">
                    <uui-button
                        color="danger"
                        look="primary"
                        @click=${this.#onDelete}
                        label="Delete"
                    ></uui-button>
                    <uui-button @click=${this._rejectModal} label="Cancel"></uui-button>
                    <uui-button
                        look="primary"
                        color="positive"
                        @click=${this.#onSubmit}
                        label="Save"
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            uui-input {
                width: 100%;
            }

            .conditions-header {
                display: flex;
                justify-content: flex-end;
                align-items: center;
                margin-bottom: var(--uui-size-space-3);
            }

            #main {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .condition-row {
                display: grid;
                grid-template-columns: 1fr 1fr auto 1fr auto;
                gap: var(--uui-size-space-2);
                align-items: center;
                margin-bottom: var(--uui-size-space-2);
            }

            .empty-state {
                color: var(--uui-color-text-alt);
                font-style: italic;
                font-size: var(--uui-type-small-size);
            }
        `,
    ];
}

export default UaiOrchestrationRouterNodeEditorModalElement;
