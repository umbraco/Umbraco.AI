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
		description: "Agents hand off work via a Communication Bus",
		icon: "icon-share",
	},
	{
		id: "groupChat",
		label: "Group Chat",
		description: "Agents collaborate via a managed Communication Bus",
		icon: "icon-chat",
	},
	{
		id: "magentic",
		label: "Magentic",
		description: "A manager agent dynamically delegates to workers via Communication Bus",
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
					{ id: "node-1", type: "Start", label: "Start", x: 100, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "End", label: "End", x: 500, y: 200, config: { $type: "end" } },
				],
				edges: [],
			};

		case "sequential":
			return {
				nodes: [
					{ id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "Agent", label: "Agent 1", x: 250, y: 200, config: { $type: "agent" } },
					{ id: "node-3", type: "Agent", label: "Agent 2", x: 500, y: 200, config: { $type: "agent" } },
					{ id: "node-4", type: "End", label: "End", x: 750, y: 200, config: { $type: "end" } },
				],
				edges: [
					{ id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false },
					{ id: "edge-3", sourceNodeId: "node-3", targetNodeId: "node-4", isDefault: false },
				],
			};

		case "concurrent":
			return {
				nodes: [
					{ id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "Agent", label: "Agent 1", x: 300, y: 50, config: { $type: "agent" } },
					{ id: "node-3", type: "Agent", label: "Agent 2", x: 300, y: 200, config: { $type: "agent" } },
					{ id: "node-4", type: "Agent", label: "Agent 3", x: 300, y: 350, config: { $type: "agent" } },
					{ id: "node-5", type: "Aggregator", label: "Aggregator", x: 550, y: 200, config: { $type: "aggregator", aggregationStrategy: "Concat" } },
					{ id: "node-6", type: "End", label: "End", x: 800, y: 200, config: { $type: "end" } },
				],
				edges: [
					{ id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-2", sourceNodeId: "node-1", targetNodeId: "node-3", isDefault: false },
					{ id: "edge-3", sourceNodeId: "node-1", targetNodeId: "node-4", isDefault: false },
					{ id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: false },
					{ id: "edge-5", sourceNodeId: "node-3", targetNodeId: "node-5", isDefault: false },
					{ id: "edge-6", sourceNodeId: "node-4", targetNodeId: "node-5", isDefault: false },
					{ id: "edge-7", sourceNodeId: "node-5", targetNodeId: "node-6", isDefault: false },
				],
			};

		case "handoff":
			return {
				nodes: [
					{ id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "CommunicationBus", label: "Handoff Bus", x: 300, y: 200, config: { $type: "communicationBus", maxIterations: 40 } },
					{ id: "node-3", type: "Agent", label: "Agent 1", x: 550, y: 100, config: { $type: "agent" } },
					{ id: "node-4", type: "Agent", label: "Agent 2", x: 550, y: 300, config: { $type: "agent" } },
					{ id: "node-5", type: "End", label: "End", x: 800, y: 200, config: { $type: "end" } },
				],
				edges: [
					{ id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false },
					{ id: "edge-3", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: false },
					{ id: "edge-4", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-5", sourceNodeId: "node-4", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-6", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: true },
				],
			};

		case "groupChat":
			return {
				nodes: [
					{ id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "CommunicationBus", label: "Group Chat", x: 300, y: 200, config: { $type: "communicationBus", maxIterations: 40 } },
					{ id: "node-3", type: "Agent", label: "Manager", x: 550, y: 50, config: { $type: "agent", isManager: true } },
					{ id: "node-4", type: "Agent", label: "Agent 1", x: 550, y: 200, config: { $type: "agent" } },
					{ id: "node-5", type: "Agent", label: "Agent 2", x: 550, y: 350, config: { $type: "agent" } },
					{ id: "node-6", type: "End", label: "End", x: 800, y: 200, config: { $type: "end" } },
				],
				edges: [
					{ id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false },
					{ id: "edge-3", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: false },
					{ id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: false },
					{ id: "edge-5", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-6", sourceNodeId: "node-4", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-7", sourceNodeId: "node-5", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-8", sourceNodeId: "node-2", targetNodeId: "node-6", isDefault: true },
				],
			};

		case "magentic":
			return {
				nodes: [
					{ id: "node-1", type: "Start", label: "Start", x: 50, y: 200, config: { $type: "start" } },
					{ id: "node-2", type: "CommunicationBus", label: "Magentic Bus", x: 300, y: 200, config: { $type: "communicationBus", maxIterations: 40 } },
					{ id: "node-3", type: "Agent", label: "Manager", x: 550, y: 50, config: { $type: "agent", isManager: true } },
					{ id: "node-4", type: "Agent", label: "Worker 1", x: 550, y: 200, config: { $type: "agent" } },
					{ id: "node-5", type: "Agent", label: "Worker 2", x: 550, y: 350, config: { $type: "agent" } },
					{ id: "node-6", type: "End", label: "End", x: 800, y: 200, config: { $type: "end" } },
				],
				edges: [
					{ id: "edge-1", sourceNodeId: "node-1", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-2", sourceNodeId: "node-2", targetNodeId: "node-3", isDefault: false },
					{ id: "edge-3", sourceNodeId: "node-2", targetNodeId: "node-4", isDefault: false },
					{ id: "edge-4", sourceNodeId: "node-2", targetNodeId: "node-5", isDefault: false },
					{ id: "edge-5", sourceNodeId: "node-3", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-6", sourceNodeId: "node-4", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-7", sourceNodeId: "node-5", targetNodeId: "node-2", isDefault: false },
					{ id: "edge-8", sourceNodeId: "node-2", targetNodeId: "node-6", isDefault: true },
				],
			};

		default:
			return { nodes: [], edges: [] };
	}
}
