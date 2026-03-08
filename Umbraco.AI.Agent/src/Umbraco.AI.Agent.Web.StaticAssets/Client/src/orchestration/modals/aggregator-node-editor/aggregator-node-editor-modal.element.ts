import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiOrchestrationAggregatorNodeEditorModalData,
    UaiOrchestrationAggregatorNodeEditorModalValue,
} from "./aggregator-node-editor-modal.token.js";
import type { UaiOrchestrationNode, UaiAggregatorNodeConfig } from "../../types.js";

const STRATEGIES = [
    { name: "Concatenate", value: "Concat" },
    { name: "Majority Vote", value: "Vote" },
    { name: "Summarize (LLM)", value: "Summarize" },
    { name: "Custom (AI Tool)", value: "Custom" },
];

/**
 * Modal for editing Aggregator node configuration.
 * Allows selecting an aggregation strategy.
 */
@customElement("uai-orchestration-aggregator-node-editor-modal")
export class UaiOrchestrationAggregatorNodeEditorModalElement extends UmbModalBaseElement<
    UaiOrchestrationAggregatorNodeEditorModalData,
    UaiOrchestrationAggregatorNodeEditorModalValue
> {
    @state()
    private _node!: UaiOrchestrationNode;

    connectedCallback() {
        super.connectedCallback();
        if (this.data?.node) {
            this._node = structuredClone(this.data.node);
        }
    }

    #onLabelChange(event: Event) {
        this._node = { ...this._node, label: (event.target as HTMLInputElement).value };
    }

    get #config(): UaiAggregatorNodeConfig {
        return this._node.config as UaiAggregatorNodeConfig;
    }

    #onStrategyChange(event: Event) {
        const value = (event.target as HTMLSelectElement).value;
        const config: UaiAggregatorNodeConfig = { ...this.#config, aggregationStrategy: value || null };
        this._node = { ...this._node, config };
    }

    #onDelete() {
        this.value = { node: this._node, deleted: true };
        this.modalContext?.submit();
    }

    #onSubmit() {
        this.value = { node: this._node };
        this.modalContext?.submit();
    }

    render() {
        if (!this._node) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-body-layout headline="Aggregator Node">
                <div id="main">
                    <uui-box>
                        <umb-property-layout label="Label" description="Display name for this node">
                            <uui-input
                                slot="editor"
                                .value=${this._node.label}
                                @input=${this.#onLabelChange}
                                placeholder="Aggregator"
                            ></uui-input>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Strategy"
                            description="How to merge results from concurrent branches"
                        >
                            <uui-select
                                slot="editor"
                                .options=${STRATEGIES.map((s) => ({
                                    ...s,
                                    selected: s.value === (this.#config.aggregationStrategy ?? "Concat"),
                                }))}
                                @change=${this.#onStrategyChange}
                            ></uui-select>
                        </umb-property-layout>
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
        `,
    ];
}

export default UaiOrchestrationAggregatorNodeEditorModalElement;
