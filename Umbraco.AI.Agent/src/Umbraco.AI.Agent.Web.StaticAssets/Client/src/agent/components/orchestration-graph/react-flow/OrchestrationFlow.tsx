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
} from "../../../types.js";
import { getNodeColor, getNodeIcon } from "../node-definitions.js";

// ── Mapping helpers ─────────────────────────────────────────────────────

function domainNodeToFlow(n: UaiOrchestrationNode): Node {
    return {
        id: n.id,
        type: "orchestration",
        position: { x: n.x, y: n.y },
        data: {
            label: n.label,
            nodeType: n.type,
            color: getNodeColor(n.type) ?? "#64748b",
            icon: getNodeIcon(n.type),
            config: n.config ?? {},
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
        data: {
            isDefault: e.isDefault,
            priority: e.priority,
        },
    };
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
                config: d.config ?? {},
            };
        }),
        edges: edges.map((e, idx) => ({
            id: e.id || `edge-${idx}`,
            sourceNodeId: e.source,
            targetNodeId: e.target,
            isDefault: (e.data as any)?.isDefault ?? false,
            priority: (e.data as any)?.priority ?? null,
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
}

// ── Component (inner, needs ReactFlowProvider ancestor) ─────────────────

const nodeTypes: NodeTypes = { orchestration: OrchestrationNodeComponent };

let edgeCounter = 0;

const OrchestrationFlowInner = forwardRef<
    OrchestrationFlowHandle,
    OrchestrationFlowProps
>(function OrchestrationFlowInner(
    { initialGraph, onGraphChanged, onNodeClicked },
    ref,
) {
    const reactFlow = useReactFlow();

    const initialNodes = useMemo(
        () => initialGraph.nodes.map(domainNodeToFlow),
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

    // Keep refs so callbacks always see latest state
    const nodesRef = useRef(nodes);
    nodesRef.current = nodes;
    const edgesRef = useRef(edges);
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

    const onConnect: OnConnect = useCallback(
        (connection: Connection) => {
            edgeCounter++;
            const newEdge: Edge = {
                ...connection,
                id: `edge-new-${edgeCounter}`,
                markerEnd: {
                    type: MarkerType.ArrowClosed,
                    width: 16,
                    height: 16,
                },
                style: { strokeWidth: 2 },
                data: { isDefault: false, priority: null },
            };
            setEdges((eds) => addEdge(newEdge, eds));
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

    // Handle Delete/Backspace for selected nodes
    const onKeyDown = useCallback(
        (event: React.KeyboardEvent) => {
            if (event.key !== "Delete" && event.key !== "Backspace") return;
            const selected = nodesRef.current.filter((n) => n.selected);
            const toRemove = selected.filter((n) => {
                const d = n.data as unknown as OrchestrationNodeData;
                return d.nodeType !== "Start" && d.nodeType !== "End";
            });
            if (toRemove.length === 0) return;
            const removeIds = new Set(toRemove.map((n) => n.id));
            setNodes((nds) => nds.filter((n) => !removeIds.has(n.id)));
            setEdges((eds) =>
                eds.filter(
                    (e) => !removeIds.has(e.source) && !removeIds.has(e.target),
                ),
            );
            emitChange();
        },
        [setNodes, setEdges, emitChange],
    );

    // Expose imperative methods to bridge
    useImperativeHandle(
        ref,
        () => ({
            addNode(node: UaiOrchestrationNode) {
                const flowNode = domainNodeToFlow(node);

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
                                      config: node.config ?? {},
                                  },
                              }
                            : n,
                    ),
                );
                emitChange();
            },
        }),
        [reactFlow, setNodes, setEdges, emitChange],
    );

    // Minimap node color
    const minimapNodeColor = useCallback((node: Node) => {
        const d = node.data as unknown as OrchestrationNodeData;
        return d.color ?? "#64748b";
    }, []);

    return (
        <div
            style={{ width: "100%", height: "100%" }}
            onKeyDown={onKeyDown}
            tabIndex={0}
        >
            <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={handleNodesChange}
                onEdgesChange={handleEdgesChange}
                onConnect={onConnect}
                onNodeDoubleClick={onNodeDoubleClick}
                nodeTypes={nodeTypes}
                fitView
                fitViewOptions={{ padding: 0.5, maxZoom: 1 }}
                deleteKeyCode={null}
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
