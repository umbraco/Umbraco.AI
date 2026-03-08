import React, {
    useCallback,
    useImperativeHandle,
    forwardRef,
    useMemo,
    useRef,
} from "react";
import {
    ReactFlow,
    MiniMap,
    Controls,
    Background,
    BackgroundVariant,
    useNodesState,
    useEdgesState,
    addEdge,
    useReactFlow,
    MarkerType,
    Position,
    type Node,
    type Edge,
    type OnConnect,
    type NodeTypes,
    type Connection,
} from "@xyflow/react";
import OrchestrationNodeComponent, {
    type OrchestrationNodeData,
} from "./OrchestrationNode.js";
import type {
    UaiOrchestrationGraph,
    UaiOrchestrationNode,
    UaiOrchestrationEdge,
    UaiNodeConfig,
} from "../../../types.js";
import { createDefaultNodeConfig } from "../../../types.js";
import { getNodeColor, getNodeIcon } from "../node-definitions.js";

// ── Mapping helpers ─────────────────────────────────────────────────────

function domainNodeToFlow(
    n: UaiOrchestrationNode,
    onEdit?: (nodeId: string) => void,
    onDelete?: (nodeId: string) => void,
): Node {
    return {
        id: n.id,
        type: "orchestration",
        position: { x: n.x, y: n.y },
        data: {
            label: n.label,
            nodeType: n.type,
            color: getNodeColor(n.type) ?? "#64748b",
            icon: getNodeIcon(n.type),
            config: ensureTypedConfig(n.config, n.type),
            onEdit,
            onDelete,
        } satisfies OrchestrationNodeData,
        sourcePosition: Position.Bottom,
        targetPosition: Position.Top,
    };
}

function domainEdgeToFlow(e: UaiOrchestrationEdge): Edge {
    return {
        id: e.id,
        source: e.sourceNodeId,
        target: e.targetNodeId,
        markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16 },
        style: { strokeWidth: 2 },
        label: e.condition?.label || (e.requiresApproval ? "Approval" : undefined),
        data: {
            isDefault: e.isDefault,
            priority: e.priority,
            condition: e.condition ?? null,
            requiresApproval: e.requiresApproval ?? false,
        },
    };
}

/**
 * Ensure a config object has a $type discriminator.
 * Falls back to creating a default config for the node type if missing.
 */
function ensureTypedConfig(config: UaiNodeConfig | Record<string, unknown> | undefined, nodeType: string): UaiNodeConfig {
    if (config && "$type" in config && config.$type) {
        return config as UaiNodeConfig;
    }
    return createDefaultNodeConfig(nodeType);
}

function flowNodesToGraph(
    nodes: Node[],
    edges: Edge[],
): UaiOrchestrationGraph {
    return {
        nodes: nodes.map((n) => {
            const d = n.data as unknown as OrchestrationNodeData;
            return {
                id: n.id,
                type: d.nodeType,
                label: d.label,
                x: n.position.x,
                y: n.position.y,
                config: ensureTypedConfig(d.config, d.nodeType),
            };
        }),
        edges: edges.map((e, idx) => ({
            id: e.id || `edge-${idx}`,
            sourceNodeId: e.source,
            targetNodeId: e.target,
            isDefault: (e.data as any)?.isDefault ?? false,
            priority: (e.data as any)?.priority ?? null,
            condition: (e.data as any)?.condition ?? null,
            requiresApproval: (e.data as any)?.requiresApproval ?? false,
        })),
    };
}

// ── Public handle ───────────────────────────────────────────────────────

export interface OrchestrationFlowHandle {
    addNode(node: UaiOrchestrationNode): void;
    removeNode(nodeId: string): void;
    updateNodeData(node: UaiOrchestrationNode): void;
}

// ── Props ───────────────────────────────────────────────────────────────

interface OrchestrationFlowProps {
    initialGraph: UaiOrchestrationGraph;
    onGraphChanged: (graph: UaiOrchestrationGraph) => void;
    onNodeClicked: (nodeId: string, nodeType: string) => void;
    onNodeEdit: (nodeId: string, nodeType: string) => void;
    onNodeDelete: (nodeId: string, nodeType: string) => void;
}

// ── Component (inner, needs ReactFlowProvider ancestor) ─────────────────

const nodeTypes: NodeTypes = { orchestration: OrchestrationNodeComponent };

let edgeCounter = 0;

const OrchestrationFlowInner = forwardRef<
    OrchestrationFlowHandle,
    OrchestrationFlowProps
>(function OrchestrationFlowInner(
    { initialGraph, onGraphChanged, onNodeClicked, onNodeEdit, onNodeDelete },
    ref,
) {
    const reactFlow = useReactFlow();

    // Refs declared early so callbacks can reference them
    const nodesRef = useRef<Node[]>([]);
    const edgesRef = useRef<Edge[]>([]);

    // Stable callbacks for node edit/delete (used by OrchestrationNode buttons)
    const handleNodeEdit = useCallback(
        (nodeId: string) => {
            const node = nodesRef.current.find((n) => n.id === nodeId);
            if (!node) return;
            const d = node.data as unknown as OrchestrationNodeData;
            onNodeEdit(nodeId, d.nodeType);
        },
        [onNodeEdit],
    );

    const handleNodeDelete = useCallback(
        (nodeId: string) => {
            const node = nodesRef.current.find((n) => n.id === nodeId);
            if (!node) return;
            const d = node.data as unknown as OrchestrationNodeData;
            onNodeDelete(nodeId, d.nodeType);
        },
        [onNodeDelete],
    );

    const initialNodes = useMemo(
        () => initialGraph.nodes.map((n) => domainNodeToFlow(n, handleNodeEdit, handleNodeDelete)),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    );
    const initialEdges = useMemo(
        () => initialGraph.edges.map(domainEdgeToFlow),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    );

    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

    // Keep refs synced so callbacks always see latest state
    nodesRef.current = nodes;
    edgesRef.current = edges;

    const emitChange = useCallback(() => {
        // Use a microtask so React state has settled
        queueMicrotask(() => {
            onGraphChanged(flowNodesToGraph(nodesRef.current, edgesRef.current));
        });
    }, [onGraphChanged]);

    // Wrap onNodesChange to emit graph updates
    const handleNodesChange: typeof onNodesChange = useCallback(
        (changes) => {
            onNodesChange(changes);
            // Only emit on position changes or removes (not selections)
            const hasMeaningfulChange = changes.some(
                (c) =>
                    c.type === "position" ||
                    c.type === "remove" ||
                    c.type === "add",
            );
            if (hasMeaningfulChange) emitChange();
        },
        [onNodesChange, emitChange],
    );

    const handleEdgesChange: typeof onEdgesChange = useCallback(
        (changes) => {
            onEdgesChange(changes);
            const hasMeaningfulChange = changes.some(
                (c) => c.type === "remove" || c.type === "add",
            );
            if (hasMeaningfulChange) emitChange();
        },
        [onEdgesChange, emitChange],
    );

    // Connection validation: no incoming to Start, no outgoing from End
    const isValidConnection = useCallback(
        (connection: Connection | Edge) => {
            const sourceNode = nodesRef.current.find((n) => n.id === connection.source);
            const targetNode = nodesRef.current.find((n) => n.id === connection.target);
            if (!sourceNode || !targetNode) return false;
            const sourceType = (sourceNode.data as unknown as OrchestrationNodeData).nodeType;
            const targetType = (targetNode.data as unknown as OrchestrationNodeData).nodeType;
            // Cannot connect FROM End nodes or TO Start nodes
            if (sourceType === "End") return false;
            if (targetType === "Start") return false;
            return true;
        },
        [],
    );

    const onConnect: OnConnect = useCallback(
        (connection: Connection) => {
            const defaultEdgeProps = {
                markerEnd: {
                    type: MarkerType.ArrowClosed,
                    width: 16,
                    height: 16,
                },
                style: { strokeWidth: 2 },
                data: { isDefault: false, priority: null },
            };

            edgeCounter++;
            const newEdge: Edge = {
                ...connection,
                id: `edge-new-${edgeCounter}`,
                ...defaultEdgeProps,
            };

            // Auto-create reverse edge for Communication Bus connections.
            // A bus is bidirectional — except when connecting to End (termination).
            const sourceNode = nodesRef.current.find((n) => n.id === connection.source);
            const targetNode = nodesRef.current.find((n) => n.id === connection.target);
            const sourceType = sourceNode ? (sourceNode.data as unknown as OrchestrationNodeData).nodeType : null;
            const targetType = targetNode ? (targetNode.data as unknown as OrchestrationNodeData).nodeType : null;

            const isBusConnection = sourceType === "CommunicationBus" || targetType === "CommunicationBus";
            const connectsToEnd = targetType === "End" || sourceType === "End";
            const connectsToStart = sourceType === "Start" || targetType === "Start";

            setEdges((eds) => {
                let updated = addEdge(newEdge, eds);

                if (isBusConnection && !connectsToEnd && !connectsToStart) {
                    // Check if reverse edge already exists
                    const reverseExists = updated.some(
                        (e) => e.source === connection.target && e.target === connection.source,
                    );
                    if (!reverseExists) {
                        edgeCounter++;
                        const reverseEdge: Edge = {
                            id: `edge-new-${edgeCounter}`,
                            source: connection.target!,
                            target: connection.source!,
                            ...defaultEdgeProps,
                        };
                        updated = addEdge(reverseEdge, updated);
                    }
                }

                return updated;
            });
            emitChange();
        },
        [setEdges, emitChange],
    );

    const onNodeDoubleClick = useCallback(
        (_event: React.MouseEvent, node: Node) => {
            const d = node.data as unknown as OrchestrationNodeData;
            onNodeClicked(node.id, d.nodeType);
        },
        [onNodeClicked],
    );

    // Guard node deletion: prevent Start and last End from being deleted.
    // Edges are always deletable. React Flow calls this before any delete.
    const onBeforeDelete = useCallback(
        async ({ nodes: nodesToDelete, edges: edgesToDelete }: { nodes: Node[]; edges: Edge[] }) => {
            const allNodes = nodesRef.current;
            const endNodeCount = allNodes.filter(
                (n) => (n.data as unknown as OrchestrationNodeData).nodeType === "End",
            ).length;

            const allowedNodes = nodesToDelete.filter((n) => {
                const d = n.data as unknown as OrchestrationNodeData;
                if (d.nodeType === "Start") return false;
                if (d.nodeType === "End" && endNodeCount <= 1) return false;
                return true;
            });

            return { nodes: allowedNodes, edges: edgesToDelete };
        },
        [],
    );

    // Expose imperative methods to bridge
    useImperativeHandle(
        ref,
        () => ({
            addNode(node: UaiOrchestrationNode) {
                const flowNode = domainNodeToFlow(node, handleNodeEdit, handleNodeDelete);

                // Position at center of viewport
                const viewport = reactFlow.getViewport();
                const container = document.querySelector(
                    ".react-flow",
                ) as HTMLElement | null;
                if (container) {
                    flowNode.position = {
                        x:
                            (container.clientWidth / 2 - viewport.x) /
                            viewport.zoom,
                        y:
                            (container.clientHeight / 2 - viewport.y) /
                            viewport.zoom,
                    };
                }

                setNodes((nds) => [...nds, flowNode]);
                emitChange();
            },

            removeNode(nodeId: string) {
                setNodes((nds) => nds.filter((n) => n.id !== nodeId));
                setEdges((eds) =>
                    eds.filter(
                        (e) => e.source !== nodeId && e.target !== nodeId,
                    ),
                );
                emitChange();
            },

            updateNodeData(node: UaiOrchestrationNode) {
                setNodes((nds) =>
                    nds.map((n) =>
                        n.id === node.id
                            ? {
                                  ...n,
                                  data: {
                                      ...n.data,
                                      label: node.label,
                                      nodeType: node.type,
                                      color:
                                          getNodeColor(node.type) ?? "#64748b",
                                      icon: getNodeIcon(node.type),
                                      config: ensureTypedConfig(node.config, node.type),
                                      onEdit: handleNodeEdit,
                                      onDelete: handleNodeDelete,
                                  },
                              }
                            : n,
                    ),
                );
                emitChange();
            },
        }),
        [reactFlow, setNodes, setEdges, emitChange, handleNodeEdit, handleNodeDelete],
    );

    // Minimap node color
    const minimapNodeColor = useCallback((node: Node) => {
        const d = node.data as unknown as OrchestrationNodeData;
        return d.color ?? "#64748b";
    }, []);

    return (
        <div
            style={{ width: "100%", height: "100%" }}
            tabIndex={0}
        >
            <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={handleNodesChange}
                onEdgesChange={handleEdgesChange}
                onConnect={onConnect}
                onNodeDoubleClick={onNodeDoubleClick}
                isValidConnection={isValidConnection}
                onBeforeDelete={onBeforeDelete}
                nodeTypes={nodeTypes}
                fitView
                fitViewOptions={{ padding: 0.5, maxZoom: 1 }}
                deleteKeyCode="Delete"
                proOptions={{ hideAttribution: true }}
                defaultEdgeOptions={{
                    markerEnd: {
                        type: MarkerType.ArrowClosed,
                        width: 16,
                        height: 16,
                    },
                    style: { strokeWidth: 2 },
                }}
            >
                <Background
                    variant={BackgroundVariant.Dots}
                    gap={20}
                    size={1}
                    color="var(--uui-color-border, #cbd5e1)"
                />
                <Controls showInteractive={false} />
                <MiniMap
                    nodeColor={minimapNodeColor}
                    maskColor="rgba(0,0,0,0.1)"
                    style={{
                        borderRadius: 4,
                        border: "1px solid var(--uui-color-border, #e2e8f0)",
                    }}
                />
            </ReactFlow>
        </div>
    );
});

export default OrchestrationFlowInner;
