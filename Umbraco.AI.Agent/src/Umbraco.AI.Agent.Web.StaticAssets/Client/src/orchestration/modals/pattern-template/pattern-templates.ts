import type { UaiOrchestrationGraph } from "../../types.js";
import type { OrchestrationPatternTemplate } from "./pattern-template-modal.token.js";

export interface PatternTemplateInfo {
    id: OrchestrationPatternTemplate;
    label: string;
    description: string;
    icon: string;
}

export const PATTERN_TEMPLATES: PatternTemplateInfo[] = [
    {
        id: "blank",
        label: "Blank",
        description: "Empty canvas with Start and End nodes",
        icon: "icon-document",
    },
    {
        id: "sequential",
        label: "Sequential",
        description: "Agents execute one after another in a pipeline",
        icon: "icon-arrow-right",
    },
    {
        id: "concurrent",
        label: "Concurrent",
        description: "Multiple agents run in parallel, results aggregated",
        icon: "icon-split",
    },
    {
        id: "handoff",
        label: "Handoff",
        description: "Agents hand off work to each other based on context",
        icon: "icon-share",
    },
    {
        id: "groupChat",
        label: "Group Chat",
        description: "Agents collaborate in a discussion loop",
        icon: "icon-chat",
    },
    {
        id: "magentic",
        label: "Magentic",
        description: "A manager agent dynamically delegates to workers",
        icon: "icon-crown",
    },
];

/**
 * Generate a pre-populated graph for the given pattern template.
 */
export function generateTemplateGraph(template: OrchestrationPatternTemplate): UaiOrchestrationGraph {
    switch (template) {
        case "blank":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 100, y: 200, config: {} },
                    { id: "node-2", type: "End", label: "End", x: 500, y: 200, config: {} },
                ],
                edges: [],
            };

        case "sequential":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: {} },
                    { id: "node-2", type: "Agent", label: "Agent 1", x: 250, y: 200, config: {} },
                    { id: "node-3", type: "Agent", label: "Agent 2", x: 500, y: 200, config: {} },
                    { id: "node-4", type: "End", label: "End", x: 750, y: 200, config: {} },
                ],
                edges: [
                    { id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false, priority: null },
                    { id: "edge-3", sourceNodeId: "node-3", targetNodeId: "node-4", isDefault: false, priority: null },
                ],
            };

        case "concurrent":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: {} },
                    { id: "node-2", type: "Agent", label: "Agent 1", x: 300, y: 50, config: {} },
                    { id: "node-3", type: "Agent", label: "Agent 2", x: 300, y: 200, config: {} },
                    { id: "node-4", type: "Agent", label: "Agent 3", x: 300, y: 350, config: {} },
                    { id: "node-5", type: "Aggregator", label: "Aggregator", x: 550, y: 200, config: { aggregationStrategy: "Concat" } },
                    { id: "node-6", type: "End", label: "End", x: 800, y: 200, config: {} },
                ],
                edges: [
                    { id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-2", sourceNodeId: "node-1", targetNodeId: "node-3", isDefault: false, priority: null },
                    { id: "edge-3", sourceNodeId: "node-1", targetNodeId: "node-4", isDefault: false, priority: null },
                    { id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: false, priority: null },
                    { id: "edge-5", sourceNodeId: "node-3", targetNodeId: "node-5", isDefault: false, priority: null },
                    { id: "edge-6", sourceNodeId: "node-4", targetNodeId: "node-5", isDefault: false, priority: null },
                    { id: "edge-7", sourceNodeId: "node-5", targetNodeId: "node-6", isDefault: false, priority: null },
                ],
            };

        case "handoff":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: {} },
                    { id: "node-2", type: "Agent", label: "Agent 1", x: 300, y: 100, config: {} },
                    { id: "node-3", type: "Agent", label: "Agent 2", x: 300, y: 300, config: {} },
                    { id: "node-4", type: "End", label: "End", x: 550, y: 200, config: {} },
                ],
                edges: [
                    { id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false, priority: null },
                    { id: "edge-3", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: true, priority: null },
                    { id: "edge-5", sourceNodeId: "node-3", targetNodeId: "node-4", isDefault: true, priority: null },
                ],
            };

        case "groupChat":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: {} },
                    { id: "node-2", type: "Agent", label: "Agent 1", x: 300, y: 100, config: {} },
                    { id: "node-3", type: "Agent", label: "Agent 2", x: 300, y: 300, config: {} },
                    { id: "node-4", type: "End", label: "End", x: 550, y: 200, config: {} },
                ],
                edges: [
                    { id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false, priority: null },
                    { id: "edge-3", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: true, priority: null },
                ],
            };

        case "magentic":
            return {
                nodes: [
                    { id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: {} },
                    { id: "node-2", type: "Manager", label: "Manager", x: 300, y: 200, config: {} },
                    { id: "node-3", type: "Agent", label: "Worker 1", x: 550, y: 100, config: {} },
                    { id: "node-4", type: "Agent", label: "Worker 2", x: 550, y: 300, config: {} },
                    { id: "node-5", type: "End", label: "End", x: 800, y: 200, config: {} },
                ],
                edges: [
                    { id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false, priority: null },
                    { id: "edge-3", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: false, priority: null },
                    { id: "edge-4", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-5", sourceNodeId: "node-4", targetNodeId: "node-2", isDefault: false, priority: null },
                    { id: "edge-6", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: true, priority: null },
                ],
            };

        default:
            return { nodes: [], edges: [] };
    }
}
