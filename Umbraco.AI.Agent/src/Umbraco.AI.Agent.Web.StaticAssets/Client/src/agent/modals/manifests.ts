import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

const modals: Array<ManifestModal> = [
	{
		type: "modal",
		alias: "UmbracoAIAgent.Modal.Agent.CreateOptions",
		name: "Agent Create Options Modal",
		js: () => import("./create-options/agent-create-options-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.ToolPermissionsOverrideEditor",
		name: "Tool Permissions Override Editor Modal",
		js: () => import("./tool-permissions-override-editor/tool-permissions-override-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationAgentNodeEditor",
		name: "Orchestration Agent Node Editor Modal",
		js: () => import("./agent-node-editor/agent-node-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationToolCallNodeEditor",
		name: "Orchestration Tool Call Node Editor Modal",
		js: () => import("./tool-call-node-editor/tool-call-node-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationRouterNodeEditor",
		name: "Orchestration Router Node Editor Modal",
		js: () => import("./router-node-editor/router-node-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationAggregatorNodeEditor",
		name: "Orchestration Aggregator Node Editor Modal",
		js: () => import("./aggregator-node-editor/aggregator-node-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationCommunicationBusNodeEditor",
		name: "Orchestration Communication Bus Node Editor Modal",
		js: () => import("./communication-bus-node-editor/communication-bus-node-editor-modal.element.js"),
	},
	{
		type: "modal",
		alias: "Uai.Modal.OrchestrationPatternTemplate",
		name: "Orchestration Pattern Template Modal",
		js: () => import("./pattern-template/pattern-template-modal.element.js"),
	},
];

export const agentModalManifests = [...modals];
