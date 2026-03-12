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
];

export const agentModalManifests = [...modals];
