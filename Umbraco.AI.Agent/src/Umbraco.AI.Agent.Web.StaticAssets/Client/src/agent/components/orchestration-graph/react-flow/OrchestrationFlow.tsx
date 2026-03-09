import React, {
    useCallback,
    useEffect,
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
    BaseEdge,
    EdgeLabelRenderer,
    getBezierPath,
    type Node,
    type Edge,
    type EdgeProps,
    type OnConnect,
    type NodeTypes,
    type EdgeTypes,
    type Connection,
    ConnectionMode,
} from "@xyflow/react";
import OrchestrationNodeComponent, {
    type OrchestrationNodeData,
} from "./OrchestrationNode.js";
import type {
    UaiOrchestrationGraph,
    UaiOrchestrationNode,
    UaiOrchestrationEdge,
    UaiOrchestrationRouteCondition,
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

/**
 * Edge label metadata: icon name + text for rendering via custom edge labels.
 */
interface EdgeLabelInfo {
    icon: string;
    text: string;
}

/**
 * Build edge label info based on condition, default status, and whether it's a router edge.
 */
function buildEdgeLabelInfo(
    condition: UaiOrchestrationRouteCondition | null | undefined,
    isDefault: boolean,
    requiresApproval: boolean,
    isRouterEdge: boolean,
): EdgeLabelInfo | undefined {
    if (condition?.label) {
        return { icon: isDefault ? "icon-block" : "icon-split-alt", text: condition.label };
    }
    if (isDefault) return { icon: "icon-block", text: "Default" };
    if (requiresApproval) return { icon: "icon-lock", text: "Approval" };
    if (isRouterEdge) return { icon: "icon-settings", text: "Set condition…" };
    return undefined;
}

// ── Custom edge with HTML label via EdgeLabelRenderer ────────────────

const edgeLabelStyle: React.CSSProperties = {
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    fontSize: 11,
    fontFamily: "var(--uui-font-family, sans-serif)",
    background: "#fff",
    border: "1px solid #e2e8f0",
    borderRadius: 4,
    padding: "2px 8px",
    pointerEvents: "all",
    whiteSpace: "nowrap",
};

function OrchestrationEdge({
    id,
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
    style,
    markerEnd,
    markerStart,
    data,
}: EdgeProps) {
    const [edgePath, labelX, labelY] = getBezierPath({
        sourceX,
        sourceY,
        sourcePosition,
        targetX,
        targetY,
        targetPosition,
    });

    const labelInfo = data?.labelInfo as EdgeLabelInfo | undefined;

    return (
        <>
            <BaseEdge
                id={id}
                path={edgePath}
                style={style}
                markerEnd={markerEnd}
                markerStart={markerStart}
            />
            {labelInfo && (
                <EdgeLabelRenderer>
                    <div
                        style={{
                            ...edgeLabelStyle,
                            position: "absolute",
                            transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
                        }}
                        className="nodrag nopan"
                    >
                        <uui-icon name={labelInfo.icon} style={{ fontSize: 12 } as React.CSSProperties}></uui-icon>
                        {labelInfo.text}
                    </div>
                </EdgeLabelRenderer>
            )}
        </>
    );
}

/**
 * Convert domain edges to flow edges.
 * Collapses reverse-pair edges (A→B + B→A) into a single bidirectional edge
 * with arrows on both ends, to avoid visual clutter on Communication Bus connections.
 */
function domainEdgesToFlow(
    edges: UaiOrchestrationEdge[],
    nodes: UaiOrchestrationNode[],
): Edge[] {
    const result: Edge[] = [];
    const consumed = new Set<string>();

    for (const e of edges) {
        if (consumed.has(e.id)) continue;

        // Look for a matching reverse edge
        const reverse = edges.find(
            (r) =>
                r.id !== e.id &&
                !consumed.has(r.id) &&
                r.sourceNodeId === e.targetNodeId &&
                r.targetNodeId === e.sourceNodeId,
        );

        const isBidirectional = !!reverse;

        if (reverse) {
            consumed.add(reverse.id);
        }

        result.push({
            id: e.id,
            type: "orchestration",
            source: e.sourceNodeId,
            target: e.targetNodeId,
            sourceHandle: e.sourceHandle ?? undefined,
            targetHandle: e.targetHandle ?? undefined,
            markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16 },
            ...(isBidirectional
                ? { markerStart: { type: MarkerType.ArrowClosed, width: 16, height: 16 } }
                : {}),
            style: { strokeWidth: 2 },
            data: {
                isDefault: e.isDefault,
                priority: e.priority,
                condition: e.condition ?? null,
                requiresApproval: e.requiresApproval ?? false,
                isBidirectional,
                reverseEdgeId: reverse?.id ?? null,
                labelInfo: buildEdgeLabelInfo(
                    e.condition,
                    e.isDefault,
                    e.requiresApproval ?? false,
                    nodes.find((n) => n.id === e.sourceNodeId)?.type === "Router",
                ),
            },
        });
    }

    return result;
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
        edges: edges.flatMap((e, idx) => {
            const data = e.data as any;
            const forward: UaiOrchestrationEdge = {
                id: e.id || `edge-${idx}`,
                sourceNodeId: e.source,
                targetNodeId: e.target,
                sourceHandle: e.sourceHandle ?? null,
                targetHandle: e.targetHandle ?? null,
                isDefault: data?.isDefault ?? false,
                priority: data?.priority ?? null,
                condition: data?.condition ?? null,
                requiresApproval: data?.requiresApproval ?? false,
            };

            // Expand bidirectional edges into a forward + reverse pair
            if (data?.isBidirectional) {
                edgeCounter++;
                const reverse: UaiOrchestrationEdge = {
                    id: data.reverseEdgeId || `edge-rev-${edgeCounter}`,
                    sourceNodeId: e.target,
                    targetNodeId: e.source,
                    // Swap handles: the forward's target becomes the reverse's source and vice versa
                    sourceHandle: e.targetHandle ?? null,
                    targetHandle: e.sourceHandle ?? null,
                    isDefault: false,
                };
                return [forward, reverse];
            }

            return [forward];
        }),
    };
}

// ── Public handle ───────────────────────────────────────────────────────

export interface OrchestrationFlowHandle {
    addNode(node: UaiOrchestrationNode): void;
    removeNode(nodeId: string): void;
    updateNodeData(node: UaiOrchestrationNode): void;
    updateEdgeData(edgeId: string, data: Record<string, unknown>): void;
}

// ── Props ───────────────────────────────────────────────────────────────

interface OrchestrationFlowProps {
    initialGraph: UaiOrchestrationGraph;
    onGraphChanged: (graph: UaiOrchestrationGraph) => void;
    onNodeClicked: (nodeId: string, nodeType: string) => void;
    onNodeEdit: (nodeId: string, nodeType: string) => void;
    onNodeDelete: (nodeId: string, nodeType: string) => void;
    onEdgeDoubleClick?: (edgeId: string, sourceNodeType: string) => void;
}

// ── Component (inner, needs ReactFlowProvider ancestor) ─────────────────

const nodeTypes: NodeTypes = { orchestration: OrchestrationNodeComponent };
const edgeTypes: EdgeTypes = { orchestration: OrchestrationEdge };

let edgeCounter = 0;

const OrchestrationFlowInner = forwardRef<
    OrchestrationFlowHandle,
    OrchestrationFlowProps
>(function OrchestrationFlowInner(
    { initialGraph, onGraphChanged, onNodeClicked, onNodeEdit, onNodeDelete, onEdgeDoubleClick },
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
        () => domainEdgesToFlow(initialGraph.edges, initialGraph.nodes),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    );

    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

    // Keep refs synced so callbacks always see latest state
    nodesRef.current = nodes;
    edgesRef.current = edges;

    // Flag-based emit: set the flag, then useEffect fires after React
    // renders with settled state — avoids reading stale refs via microtask.
    const needsEmitRef = useRef(false);
    const emitChange = useCallback(() => {
        needsEmitRef.current = true;
    }, []);

    useEffect(() => {
        if (needsEmitRef.current) {
            needsEmitRef.current = false;
            onGraphChanged(flowNodesToGraph(nodes, edges));
        }
    });

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
            // Determine if this is a bidirectional bus connection
            const sourceNode = nodesRef.current.find((n) => n.id === connection.source);
            const targetNode = nodesRef.current.find((n) => n.id === connection.target);
            const sourceType = sourceNode ? (sourceNode.data as unknown as OrchestrationNodeData).nodeType : null;
            const targetType = targetNode ? (targetNode.data as unknown as OrchestrationNodeData).nodeType : null;

            const isBusConnection = sourceType === "CommunicationBus" || targetType === "CommunicationBus";
            const connectsToEnd = targetType === "End" || sourceType === "End";
            const connectsToStart = sourceType === "Start" || targetType === "Start";
            // Bidirectional for bus connections (not to/from start/end)
            const bidirectional = isBusConnection && !connectsToEnd && !connectsToStart;

            const isRouterEdge = sourceType === "Router";

            edgeCounter++;
            const newEdge: Edge = {
                ...connection,
                id: `edge-new-${edgeCounter}`,
                type: "orchestration",
                markerEnd: { type: MarkerType.ArrowClosed, width: 16, height: 16 },
                ...(bidirectional
                    ? { markerStart: { type: MarkerType.ArrowClosed, width: 16, height: 16 } }
                    : {}),
                style: { strokeWidth: 2 },
                data: {
                    isDefault: false,
                    priority: null,
                    isBidirectional: bidirectional,
                    labelInfo: buildEdgeLabelInfo(null, false, false, isRouterEdge),
                },
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

    const onEdgeDoubleClickHandler = useCallback(
        (_event: React.MouseEvent, edge: Edge) => {
            // Find the source node to determine its type
            const sourceNode = nodesRef.current.find((n) => n.id === edge.source);
            const sourceType = sourceNode ? (sourceNode.data as unknown as OrchestrationNodeData).nodeType : "";
            onEdgeDoubleClick?.(edge.id, sourceType);
        },
        [onEdgeDoubleClick],
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

            updateEdgeData(edgeId: string, data: Record<string, unknown>) {
                // Visual-only update — does NOT call emitChange().
                // The caller (Lit element) handles graph state via #onGraphChanged.
                // Calling emitChange here would read stale refs (React hasn't
                // re-rendered yet) and overwrite the graph with old data.
                setEdges((eds) =>
                    eds.map((e) => {
                        if (e.id !== edgeId) return e;
                        const merged = { ...e.data, ...data };
                        const srcNode = nodesRef.current.find((n) => n.id === e.source);
                        const isRouter = srcNode
                            ? (srcNode.data as unknown as OrchestrationNodeData).nodeType === "Router"
                            : false;
                        return {
                            ...e,
                            data: {
                                ...merged,
                                labelInfo: buildEdgeLabelInfo(
                                    merged.condition as UaiOrchestrationRouteCondition | null,
                                    merged.isDefault as boolean,
                                    merged.requiresApproval as boolean,
                                    isRouter,
                                ),
                            },
                        };
                    }),
                );
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
                onEdgeDoubleClick={onEdgeDoubleClickHandler}
                isValidConnection={isValidConnection}
                onBeforeDelete={onBeforeDelete}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                connectionMode={ConnectionMode.Loose}
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
