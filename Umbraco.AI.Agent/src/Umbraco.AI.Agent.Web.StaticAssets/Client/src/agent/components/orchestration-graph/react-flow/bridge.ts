import React from "react";
import { createRoot, type Root } from "react-dom/client";
import { ReactFlowProvider } from "@xyflow/react";
import OrchestrationFlowInner, {
    type OrchestrationFlowHandle,
} from "./OrchestrationFlow.js";
import type {
    UaiOrchestrationGraph,
    UaiOrchestrationNode,
} from "../../../types.js";

// React Flow CSS imported as inline string (see css.d.ts)
import reactFlowCss from "@xyflow/react/dist/style.css?inline";

export interface FlowBridgeInstance {
    addNode(node: UaiOrchestrationNode): void;
    removeNode(nodeId: string): void;
    updateNodeData(node: UaiOrchestrationNode): void;
    destroy(): void;
}

/**
 * Inject React Flow styles into the shadow root containing the container.
 * Uses a data attribute to avoid duplicate injection.
 */
function ensureStyles(container: HTMLElement): void {
    const root = container.getRootNode() as ShadowRoot | Document;
    if (root.querySelector("[data-react-flow-styles]")) return;

    const style = document.createElement("style");
    style.setAttribute("data-react-flow-styles", "");
    style.textContent = reactFlowCss;

    if (root instanceof ShadowRoot) {
        root.appendChild(style);
    } else {
        document.head.appendChild(style);
    }
}

/**
 * Mount a React Flow editor into a DOM container.
 * Returns an imperative handle to manipulate the graph externally.
 */
export function createFlowBridge(
    container: HTMLElement,
    graph: UaiOrchestrationGraph,
    onGraphChanged: (graph: UaiOrchestrationGraph) => void,
    onNodeClicked: (nodeId: string, nodeType: string) => void,
): FlowBridgeInstance {
    ensureStyles(container);

    const root: Root = createRoot(container);
    const flowRef = React.createRef<OrchestrationFlowHandle>();

    root.render(
        React.createElement(
            ReactFlowProvider,
            null,
            React.createElement(OrchestrationFlowInner, {
                ref: flowRef,
                initialGraph: graph,
                onGraphChanged,
                onNodeClicked,
            }),
        ),
    );

    return {
        addNode(node: UaiOrchestrationNode) {
            flowRef.current?.addNode(node);
        },
        removeNode(nodeId: string) {
            flowRef.current?.removeNode(nodeId);
        },
        updateNodeData(node: UaiOrchestrationNode) {
            flowRef.current?.updateNodeData(node);
        },
        destroy() {
            root.unmount();
        },
    };
}
