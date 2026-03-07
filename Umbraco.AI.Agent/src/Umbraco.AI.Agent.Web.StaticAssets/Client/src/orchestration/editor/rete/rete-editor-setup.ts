import { NodeEditor, ClassicPreset } from "rete";
import { AreaPlugin, AreaExtensions } from "rete-area-plugin";
import { ConnectionPlugin, Presets as ConnectionPresets } from "rete-connection-plugin";
import { LitPlugin, Presets as LitPresets } from "@retejs/lit-plugin";
import { MinimapPlugin } from "rete-minimap-plugin";
import { AutoArrangePlugin, Presets as ArrangePresets } from "rete-auto-arrange-plugin";
import type { Schemes, AreaExtra } from "./rete-types.js";
import { OrchestrationNode, OrchestrationConnection } from "./rete-types.js";
import type { UaiOrchestrationGraph, UaiOrchestrationNode, UaiOrchestrationEdge } from "../../types.js";
import { getNodeColor, getNodeIcon } from "./rete-node-definitions.js";

export interface ReteEditorInstance {
    editor: NodeEditor<Schemes>;
    area: AreaPlugin<Schemes, AreaExtra>;
    arrange: AutoArrangePlugin<Schemes>;
    destroy: () => void;
}

/**
 * Initialize a Rete.js editor in the given container element.
 */
export async function createReteEditor(
    container: HTMLElement,
    graph: UaiOrchestrationGraph,
    onGraphChanged: (graph: UaiOrchestrationGraph) => void,
    onNodeClicked: (nodeId: string, nodeType: string) => void,
): Promise<ReteEditorInstance> {
    const editor = new NodeEditor<Schemes>();
    const area = new AreaPlugin<Schemes, AreaExtra>(container);
    const connection = new ConnectionPlugin<Schemes, AreaExtra>();
    const render = new LitPlugin<Schemes, AreaExtra>();
    const minimap = new MinimapPlugin<Schemes>();
    const arrange = new AutoArrangePlugin<Schemes>();

    // Configure connection plugin
    connection.addPreset(ConnectionPresets.classic.setup());

    // Configure Lit renderer with custom node rendering
    render.addPreset(
        LitPresets.classic.setup({
            customize: {
                node(_context) {
                    return undefined; // use default rendering
                },
            },
        }),
    );

    render.addPreset(LitPresets.minimap.setup());

    // Configure auto-arrange
    arrange.addPreset(ArrangePresets.classic.setup());

    // Wire plugins
    editor.use(area);
    area.use(connection);
    area.use(render);
    area.use(minimap);
    area.use(arrange);

    // Enable zoom and selection
    AreaExtensions.simpleNodesOrder(area);
    AreaExtensions.selectableNodes(area, AreaExtensions.selector(), {
        accumulating: AreaExtensions.accumulateOnCtrl(),
    });

    // Handle node double-click for editing
    area.addPipe((context) => {
        if (context.type === "nodedblclick") {
            const nodeId = context.data.id;
            const node = editor.getNode(nodeId);
            if (node instanceof OrchestrationNode) {
                onNodeClicked(node.nodeId, node.nodeType);
            }
        }
        return context;
    });

    // Load initial graph
    await loadGraph(editor, area, graph);

    // Auto-arrange if no positions are set
    const hasPositions = graph.nodes.some((n) => n.x !== 0 || n.y !== 0);
    if (!hasPositions && graph.nodes.length > 0) {
        await arrange.layout();
        AreaExtensions.zoomAt(area, editor.getNodes());
    } else if (graph.nodes.length > 0) {
        AreaExtensions.zoomAt(area, editor.getNodes());
    }

    // Sync changes back
    const syncGraph = () => {
        const updatedGraph = exportGraph(editor, area);
        onGraphChanged(updatedGraph);
    };

    editor.addPipe((context) => {
        if (
            context.type === "noderemoved" ||
            context.type === "connectioncreated" ||
            context.type === "connectionremoved"
        ) {
            syncGraph();
        }
        return context;
    });

    area.addPipe((context) => {
        if (context.type === "nodetranslated") {
            syncGraph();
        }
        return context;
    });

    return {
        editor,
        area,
        arrange,
        destroy: () => area.destroy(),
    };
}

/**
 * Load a UaiOrchestrationGraph into the Rete editor.
 */
async function loadGraph(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>,
    graph: UaiOrchestrationGraph,
) {
    // Create nodes
    const nodeMap = new Map<string, OrchestrationNode>();

    for (const nodeData of graph.nodes) {
        const node = createReteNode(nodeData);
        await editor.addNode(node);
        nodeMap.set(nodeData.id, node);

        // Set position if available
        if (nodeData.x !== 0 || nodeData.y !== 0) {
            await area.translate(node.id, { x: nodeData.x, y: nodeData.y });
        }
    }

    // Create connections
    for (const edge of graph.edges) {
        const source = nodeMap.get(edge.sourceNodeId);
        const target = nodeMap.get(edge.targetNodeId);

        if (source && target) {
            const sourceOutput = Object.keys(source.outputs)[0];
            const targetInput = Object.keys(target.inputs)[0];

            if (sourceOutput && targetInput) {
                const conn = new OrchestrationConnection(source, sourceOutput, target, targetInput);
                conn.edgeId = edge.id;
                conn.isDefault = edge.isDefault;
                conn.priority = edge.priority ?? undefined;
                await editor.addConnection(conn);
            }
        }
    }
}

/**
 * Create a Rete node from a UaiOrchestrationNode.
 */
function createReteNode(nodeData: UaiOrchestrationNode): OrchestrationNode {
    const icon = getNodeIcon(nodeData.type);
    const label = `${icon} ${nodeData.label}`;
    const node = new OrchestrationNode(label, nodeData.type, nodeData.id);

    // Add a single output socket
    const socket = new ClassicPreset.Socket("flow");
    node.addOutput("out", new ClassicPreset.Output(socket, "Out"));
    node.addInput("in", new ClassicPreset.Input(socket, "In", true));

    // Customize dimensions based on type
    const color = getNodeColor(nodeData.type);
    if (color) {
        // Store color info for custom rendering
        (node as any)._color = color;
    }

    // Start nodes don't need inputs, End nodes don't need outputs
    if (nodeData.type === "Start") {
        node.removeInput("in");
        node.height = 60;
    } else if (nodeData.type === "End") {
        node.removeOutput("out");
        node.height = 60;
    }

    return node;
}

/**
 * Export current editor state as a UaiOrchestrationGraph.
 */
function exportGraph(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>,
): UaiOrchestrationGraph {
    const nodes: UaiOrchestrationNode[] = editor.getNodes().map((node) => {
        if (!(node instanceof OrchestrationNode)) {
            return { id: node.id, type: "Agent", label: node.label, x: 0, y: 0, config: {} };
        }
        const view = area.nodeViews.get(node.id);
        return {
            id: node.nodeId,
            type: node.nodeType,
            label: node.label.replace(/^[^\s]+ /, ""), // Remove icon prefix
            x: view?.position.x ?? 0,
            y: view?.position.y ?? 0,
            config: {}, // Config is managed through modals, not direct graph export
        };
    });

    const edges: UaiOrchestrationEdge[] = editor.getConnections().map((conn, index) => {
        const sourceNode = editor.getNode(conn.source);
        const targetNode = editor.getNode(conn.target);
        const orchConn = conn as OrchestrationConnection;
        return {
            id: orchConn.edgeId ?? `edge-${index}`,
            sourceNodeId:
                sourceNode instanceof OrchestrationNode ? sourceNode.nodeId : conn.source,
            targetNodeId:
                targetNode instanceof OrchestrationNode ? targetNode.nodeId : conn.target,
            isDefault: orchConn.isDefault ?? false,
            priority: orchConn.priority ?? null,
        };
    });

    return { nodes, edges };
}

/**
 * Add a new node to the editor.
 */
export async function addNodeToEditor(
    instance: ReteEditorInstance,
    nodeData: UaiOrchestrationNode,
): Promise<void> {
    const node = createReteNode(nodeData);
    await instance.editor.addNode(node);

    // Position at center of viewport
    const { x, y, k } = instance.area.area.transform;
    const container = instance.area.container;
    const centerX = (container.clientWidth / 2 - x) / k;
    const centerY = (container.clientHeight / 2 - y) / k;

    await instance.area.translate(node.id, { x: centerX, y: centerY });
}

/**
 * Remove a node from the editor.
 */
export async function removeNodeFromEditor(
    instance: ReteEditorInstance,
    reteNodeId: string,
): Promise<void> {
    // Remove all connections first
    const connections = instance.editor.getConnections().filter(
        (c) => c.source === reteNodeId || c.target === reteNodeId,
    );
    for (const conn of connections) {
        await instance.editor.removeConnection(conn.id);
    }
    await instance.editor.removeNode(reteNodeId);
}
