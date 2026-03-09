import { css, html, customElement, state, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiOrchestrationGraph, UaiOrchestrationNode } from "../../types.js";
import { createDefaultNodeConfig } from "../../types.js";
import type { FlowBridgeInstance } from "./react-flow/bridge.js";
import { createFlowBridge } from "./react-flow/bridge.js";
import { getAddableNodeTypes } from "./node-definitions.js";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import {
    UAI_ITEM_PICKER_MODAL,
    type UaiPickableItemModel,
} from "@umbraco-ai/core";
import {
    UAI_ORCHESTRATION_AGENT_NODE_EDITOR_MODAL,
} from "../../modals/agent-node-editor/agent-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_TOOL_CALL_NODE_EDITOR_MODAL,
} from "../../modals/tool-call-node-editor/tool-call-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_ROUTER_NODE_EDITOR_MODAL,
} from "../../modals/router-node-editor/router-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_AGGREGATOR_NODE_EDITOR_MODAL,
} from "../../modals/aggregator-node-editor/aggregator-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_COMMUNICATION_BUS_NODE_EDITOR_MODAL,
} from "../../modals/communication-bus-node-editor/communication-bus-node-editor-modal.token.js";
import {
    UAI_ORCHESTRATION_ROUTER_EDGE_CONDITION_EDITOR_MODAL,
} from "../../modals/router-edge-condition-editor/router-edge-condition-editor-modal.token.js";

/**
 * Graph editor for orchestration workflows.
 * Uses React Flow for visual node-based editing, mounted inside this Lit element's shadow DOM.
 *
 * @fires graph-changed - Dispatched when the graph structure changes
 */
@customElement("uai-orchestration-graph-editor")
export class UaiOrchestrationGraphEditorElement extends UmbLitElement {
    @property({ type: Object })
    graph: UaiOrchestrationGraph = { nodes: [], edges: [] };

    @state()
    private _bridge?: FlowBridgeInstance;

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
        requestAnimationFrame(() => {
            const container = this.renderRoot.querySelector<HTMLElement>("#flow-container");
            if (!container) return;

            try {
                this._bridge = createFlowBridge(
                    container,
                    this.graph,
                    (updatedGraph) => this.#onGraphChanged(updatedGraph),
                    (nodeId, nodeType) => this.#onNodeClicked(nodeId, nodeType),
                    (nodeId, nodeType) => this.#onNodeEditRequested(nodeId, nodeType),
                    (nodeId, nodeType) => this.#onNodeDeleteRequested(nodeId, nodeType),
                    (edgeId, sourceNodeType) => this.#onEdgeDoubleClicked(edgeId, sourceNodeType),
                );

                // Track highest node counter for generating unique IDs
                this._nodeCounter = this.graph.nodes.reduce((max, n) => {
                    const num = parseInt(n.id.replace("node-", ""), 10);
                    return isNaN(num) ? max : Math.max(max, num);
                }, 0);
            } catch (error) {
                console.error("Failed to initialize graph editor:", error);
            } finally {
                this._isLoading = false;
            }
        });
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        this._bridge?.destroy();
    }

    #onGraphChanged(updatedGraph: UaiOrchestrationGraph) {
        // Keep local copy current so lookups (node edit, edge edit) see latest state
        this.graph = updatedGraph;
        this.dispatchEvent(
            new CustomEvent("graph-changed", {
                detail: updatedGraph,
                bubbles: true,
                composed: true,
            }),
        );
    }

    async #onNodeClicked(nodeId: string, _nodeType: string) {
        const node = this.graph.nodes.find((n) => n.id === nodeId);
        if (!node) return;
        await this.#openNodeEditorModal(node);
    }

    async #onNodeEditRequested(nodeId: string, _nodeType: string) {
        const node = this.graph.nodes.find((n) => n.id === nodeId);
        if (!node) return;
        await this.#openNodeEditorModal(node);
    }

    #onNodeDeleteRequested(nodeId: string, nodeType: string) {
        // Never delete Start
        if (nodeType === "Start") return;
        // Don't delete the last End node
        if (nodeType === "End") {
            const endCount = this.graph.nodes.filter((n) => n.type === "End").length;
            if (endCount <= 1) return;
        }
        this._bridge?.removeNode(nodeId);
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

        // Create new node with typed default config
        this._nodeCounter++;
        const definition = getAddableNodeTypes().find((d) => d.type === selectedType);
        const newNode: UaiOrchestrationNode = {
            id: `node-${this._nodeCounter}`,
            type: selectedType,
            label: definition?.label ?? selectedType,
            x: 0,
            y: 0,
            config: createDefaultNodeConfig(selectedType),
        };

        this._bridge?.addNode(newNode);

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
            case "ToolCall":
                result = await openAndWait(UAI_ORCHESTRATION_TOOL_CALL_NODE_EDITOR_MODAL);
                break;
            case "Router":
                result = await openAndWait(UAI_ORCHESTRATION_ROUTER_NODE_EDITOR_MODAL);
                break;
            case "Aggregator":
                result = await openAndWait(UAI_ORCHESTRATION_AGGREGATOR_NODE_EDITOR_MODAL);
                break;
            case "CommunicationBus":
                result = await openAndWait(UAI_ORCHESTRATION_COMMUNICATION_BUS_NODE_EDITOR_MODAL);
                break;
        }

        if (result?.deleted) {
            this._bridge?.removeNode(node.id);
        } else if (result?.node) {
            this._bridge?.updateNodeData(result.node);
            this.#updateNodeInGraph(result.node);
        }
    }

    async #onEdgeDoubleClicked(edgeId: string, sourceNodeType: string) {
        // Only open condition editor for router edges
        if (sourceNodeType !== "Router") return;
        if (!this.#modalManagerContext) return;

        const edge = this.graph.edges.find((e) => e.id === edgeId);
        if (!edge) return;

        const modal = this.#modalManagerContext.open(this, UAI_ORCHESTRATION_ROUTER_EDGE_CONDITION_EDITOR_MODAL, {
            data: {
                edgeId,
                condition: edge.condition ?? null,
                isDefault: edge.isDefault,
                priority: edge.priority ?? null,
                requiresApproval: edge.requiresApproval ?? false,
            },
        });

        const result = await modal.onSubmit().catch(() => undefined);
        if (!result) return;

        // Update the edge in the graph
        const updatedGraph = structuredClone(this.graph);
        const edgeIndex = updatedGraph.edges.findIndex((e) => e.id === edgeId);
        if (edgeIndex >= 0) {
            updatedGraph.edges[edgeIndex] = {
                ...updatedGraph.edges[edgeIndex],
                condition: result.condition,
                isDefault: result.isDefault,
                priority: result.priority,
                requiresApproval: result.requiresApproval,
            };
        }

        // Update the React Flow edge visual
        this._bridge?.updateEdgeData(edgeId, {
            condition: result.condition,
            isDefault: result.isDefault,
            priority: result.priority,
            requiresApproval: result.requiresApproval,
        });

        this.#onGraphChanged(updatedGraph);
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
                <div id="flow-container" class="flow-container">
                    ${this._isLoading ? html`<uui-loader></uui-loader>` : ""}
                </div>
                <uui-button
                    class="add-node-overlay"
                    look="primary"
                    compact
                    @click=${this.#onAddNodeClicked}
                >
                    <uui-icon name="icon-add"></uui-icon>
                    Add Node
                </uui-button>
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
                position: relative;
                width: 100%;
                height: 100%;
                overflow: hidden;
            }

            .flow-container {
                position: relative;
                width: 100%;
                height: 100%;
            }

            .add-node-overlay {
                position: absolute;
                top: var(--uui-size-space-4);
                left: var(--uui-size-space-4);
                z-index: 5;
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
