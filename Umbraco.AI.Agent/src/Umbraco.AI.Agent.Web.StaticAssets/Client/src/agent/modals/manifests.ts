import type { ManifestModal } from "@umbraco-cms/backoffice/extension-registry";

const modals: Array<ManifestModal> = [
	{
		type: "modal",
		alias: "Uai.Modal.ToolPermissionsOverrideEditor",
		name: "Tool Permissions Override Editor Modal",
		js: () => import("./tool-permissions-override-editor/tool-permissions-override-editor-modal.element.js"),
	},
];

export const agentModalManifests = [...modals];
