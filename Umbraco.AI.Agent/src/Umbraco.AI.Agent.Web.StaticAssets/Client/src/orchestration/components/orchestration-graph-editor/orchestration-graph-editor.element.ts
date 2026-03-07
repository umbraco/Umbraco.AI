import { css, html, customElement, state, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiOrchestrationGraph, UaiOrchestrationNode } from "../../types.js";
import type { ReteEditorInstance } from "./rete/rete-editor-setup.js";
import { createReteEditor, addNodeToEditor, removeNodeFromEditor } from "./rete/rete-editor-setup.js";
import { getAddableNodeTypes } from "./rete/rete-node-definitions.js";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import {
    UAI_ITEM_PICKER_MODAL,
    type UaiPickableItemModel,
} from "@umbraco-ai/core";
import {
    UAI_ORCHESTRATION_AGENT_NODE_EDITOR_MODAL,
} from "../../modals/agent-node-editor/agent-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_FUNCTION_NODE_EDITOR_MODAL,
} from "../../modals/function-node-editor/function-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_ROUTER_NODE_EDITOR_MODAL,
} from "../../modals/router-node-editor/router-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_AGGREGATOR_NODE_EDITOR_MODAL,
} from "../../modals/aggregator-node-editor/aggregator-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_MANAGER_NODE_EDITOR_MODAL,
} from "../../modals/manager-node-editor/manager-node-editor-modal.token.js";

/**
 * Graph editor for orchestration workflows.
 * Uses Rete.js for visual node-based editing.
 *
 * @fires graph-changed - Dispatched when the graph structure changes
 */
@customElement("uai-orchestration-graph-editor")
export class UaiOrchestrationGraphEditorElement extends UmbLitElement {
    @property({ type: Object })
    graph: UaiOrchestrationGraph = { nodes: [], edges: [] };

    @state()
    private _editorInstance?: ReteEditorInstance;

    @state()
    private _isLoading = true;

    @state()
    private _nodeCounter = 0;

    #modalManagerContext?: typeof UMB_MODAL_MANAGER_CONTEXT.TYPE;

    constructor() {
        super();
        this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
            this.#modalManagerContext = context;
        });
    }

    async firstUpdated() {
        await this.updateComplete;

        // Wait a frame for the container to have proper dimensions
        requestAnimationFrame(async () => {
            const container = this.renderRoot.querySelector<HTMLElement>("#rete-container");
            if (!container) return;

            try {
                this._editorInstance = await createReteEditor(
                    container,
                    this.graph,
                    (updatedGraph) => this.#onGraphChanged(updatedGraph),
                    (nodeId, nodeType) => this.#onNodeClicked(nodeId, nodeType),
                );

                // Track highest node counter for generating unique IDs
                this._nodeCounter = this.graph.nodes.reduce((max, n) => {
                    const num = parseInt(n.id.replace("node-", ""), 10);
                    return isNaN(num) ? max : Math.max(max, num);
                }, 0);
            } catch (error) {
                console.error("Failed to initialize Rete editor:", error);
            } finally {
                this._isLoading = false;
            }
        });
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        this._editorInstance?.destroy();
    }

    #onGraphChanged(updatedGraph: UaiOrchestrationGraph) {
        this.dispatchEvent(
            new CustomEvent("graph-changed", {
                detail: updatedGraph,
                bubbles: true,
                composed: true,
            }),
        );
    }

    async #onNodeClicked(nodeId: string, _nodeType: string) {
        // Find the node data from the current graph
        const node = this.graph.nodes.find((n) => n.id === nodeId);
        if (!node) return;

        await this.#openNodeEditorModal(node);
    }

    async #onAddNodeClicked() {
        if (!this.#modalManagerContext) return;

        const items: UaiPickableItemModel[] = getAddableNodeTypes().map((def) => ({
            value: def.type,
            label: def.label,
            description: def.description,
            icon: def.icon,
            color: def.color,
        }));

        const modal = this.#modalManagerContext.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                items,
                selectionMode: "single",
                title: "Add Node",
            },
        });

        const result = await modal.onSubmit().catch(() => undefined);
        const selectedType = result?.selection?.[0]?.value;
        if (!selectedType) return;

        // Create new node
        this._nodeCounter++;
        const newNode: UaiOrchestrationNode = {
            id: `node-${this._nodeCounter}`,
            type: selectedType,
            label: selectedType,
            x: 0,
            y: 0,
            config: {},
        };

        if (this._editorInstance) {
            await addNodeToEditor(this._editorInstance, newNode);
        }

        // Open the editor modal for the new node
        await this.#openNodeEditorModal(newNode);
    }

    async #openNodeEditorModal(node: UaiOrchestrationNode) {
        if (!this.#modalManagerContext) return;

        const openAndWait = async (token: any) => {
            const modal = this.#modalManagerContext!.open(this, token, { data: { node } });
            return modal.onSubmit().catch(() => undefined) as Promise<
                { node: UaiOrchestrationNode; deleted?: boolean } | undefined
            >;
        };

        let result: { node: UaiOrchestrationNode; deleted?: boolean } | undefined;

        switch (node.type) {
            case "Agent":
                result = await openAndWait(UAI_ORCHESTRATION_AGENT_NODE_EDITOR_MODAL);
                break;
            case "Function":
                result = await openAndWait(UAI_ORCHESTRATION_FUNCTION_NODE_EDITOR_MODAL);
                break;
            case "Router":
                result = await openAndWait(UAI_ORCHESTRATION_ROUTER_NODE_EDITOR_MODAL);
                break;
            case "Aggregator":
                result = await openAndWait(UAI_ORCHESTRATION_AGGREGATOR_NODE_EDITOR_MODAL);
                break;
            case "Manager":
                result = await openAndWait(UAI_ORCHESTRATION_MANAGER_NODE_EDITOR_MODAL);
                break;
        }

        if (result?.deleted) {
            await this.#onDeleteNode(node.id);
        } else if (result?.node) {
            this.#updateNodeInGraph(result.node);
        }
    }

    async #onDeleteNode(nodeId: string) {
        if (!this._editorInstance) return;

        // Find the rete node by our domain node ID
        const reteNode = this._editorInstance.editor
            .getNodes()
            .find((n) => (n as any).nodeId === nodeId);
        if (!reteNode) return;

        await removeNodeFromEditor(this._editorInstance, reteNode.id);
    }

    #updateNodeInGraph(updatedNode: UaiOrchestrationNode) {
        const updatedGraph = structuredClone(this.graph);
        const index = updatedGraph.nodes.findIndex((n) => n.id === updatedNode.id);
        if (index >= 0) {
            updatedGraph.nodes[index] = { ...updatedGraph.nodes[index], ...updatedNode };
        } else {
            updatedGraph.nodes.push(updatedNode);
        }
        this.#onGraphChanged(updatedGraph);
    }

    render() {
        return html`
            <div class="editor-wrapper">
                <div class="editor-toolbar">
                    <uui-button
                        look="primary"
                        compact
                        @click=${this.#onAddNodeClicked}
                    >
                        <uui-icon name="icon-add"></uui-icon>
                        Add Node
                    </uui-button>
                    <span class="graph-stats">
                        ${this.graph.nodes.length} nodes, ${this.graph.edges.length} edges
                    </span>
                </div>
                <div id="rete-container" class="rete-container">
                    ${this._isLoading ? html`<uui-loader></uui-loader>` : ""}
                </div>
            </div>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }

            .editor-wrapper {
                display: flex;
                flex-direction: column;
                width: 100%;
                height: 100%;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                overflow: hidden;
            }

            .editor-toolbar {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-3);
                background: var(--uui-color-surface);
                border-bottom: 1px solid var(--uui-color-border);
            }

            .graph-stats {
                margin-left: auto;
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }

            .rete-container {
                position: relative;
                flex: 1;
                width: 100%;
                background-color: var(--uui-color-surface-alt);
                background-image: radial-gradient(
                    circle,
                    var(--uui-color-border) 1px,
                    transparent 1px
                );
                background-size: 20px 20px;
            }

            uui-loader {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }
        `,
    ];
}

export default UaiOrchestrationGraphEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-graph-editor": UaiOrchestrationGraphEditorElement;
    }
}
