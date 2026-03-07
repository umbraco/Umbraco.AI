/**
 * Node type definitions: icons and colors for each orchestration node type.
 */

export interface NodeTypeDefinition {
    type: string;
    label: string;
    description: string;
    icon: string;
    color: string;
}

export const NODE_TYPE_DEFINITIONS: NodeTypeDefinition[] = [
    {
        type: "Start",
        label: "Start",
        description: "Entry point of the orchestration",
        icon: "icon-play",
        color: "#22c55e",
    },
    {
        type: "End",
        label: "End",
        description: "Exit point of the orchestration",
        icon: "icon-stop",
        color: "#ef4444",
    },
    {
        type: "Agent",
        label: "Agent",
        description: "References an existing AI agent",
        icon: "icon-bot",
        color: "#3b82f6",
    },
    {
        type: "Function",
        label: "Function",
        description: "Executes a registered AI tool without LLM",
        icon: "icon-wand",
        color: "#f59e0b",
    },
    {
        type: "Router",
        label: "Router",
        description: "Conditional routing based on output rules",
        icon: "icon-split",
        color: "#8b5cf6",
    },
    {
        type: "Aggregator",
        label: "Aggregator",
        description: "Merges results from concurrent branches",
        icon: "icon-merge",
        color: "#06b6d4",
    },
    {
        type: "Manager",
        label: "Manager",
        description: "Magentic pattern: delegates work dynamically",
        icon: "icon-crown",
        color: "#ec4899",
    },
];

/**
 * Get the icon for a node type.
 */
export function getNodeIcon(nodeType: string): string {
    return NODE_TYPE_DEFINITIONS.find((d) => d.type === nodeType)?.icon ?? "icon-circle-dotted-line";
}

/**
 * Get the color for a node type.
 */
export function getNodeColor(nodeType: string): string | undefined {
    return NODE_TYPE_DEFINITIONS.find((d) => d.type === nodeType)?.color;
}

/**
 * Get the user-addable node types (excludes Start/End which are managed automatically).
 */
export function getAddableNodeTypes(): NodeTypeDefinition[] {
    return NODE_TYPE_DEFINITIONS.filter((d) => d.type !== "Start" && d.type !== "End");
}
