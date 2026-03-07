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
        icon: "\u25B6",
        color: "#4caf50",
    },
    {
        type: "End",
        label: "End",
        description: "Exit point of the orchestration",
        icon: "\u25A0",
        color: "#f44336",
    },
    {
        type: "Agent",
        label: "Agent",
        description: "References an existing AI agent",
        icon: "\uD83E\uDD16",
        color: "#2196f3",
    },
    {
        type: "Function",
        label: "Function",
        description: "Executes a registered AI tool without LLM",
        icon: "\u2699",
        color: "#ff9800",
    },
    {
        type: "Router",
        label: "Router",
        description: "Conditional routing based on output rules",
        icon: "\u2B95",
        color: "#9c27b0",
    },
    {
        type: "Aggregator",
        label: "Aggregator",
        description: "Merges results from concurrent branches",
        icon: "\u2A01",
        color: "#00bcd4",
    },
    {
        type: "Manager",
        label: "Manager",
        description: "Magentic pattern: delegates work dynamically",
        icon: "\uD83D\uDC51",
        color: "#e91e63",
    },
];

/**
 * Get the icon for a node type.
 */
export function getNodeIcon(nodeType: string): string {
    return NODE_TYPE_DEFINITIONS.find((d) => d.type === nodeType)?.icon ?? "\u2B24";
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
