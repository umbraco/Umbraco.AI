import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_TEST_WORKSPACE_ALIAS, UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: "workspace",
		kind: "routable",
		alias: UAI_TEST_WORKSPACE_ALIAS,
		name: "Test Workspace",
		api: () => import("./test-workspace.context.js"),
		meta: {
			entityType: UAI_TEST_ENTITY_TYPE,
		},
	},
	{
		type: "workspaceView",
		alias: "UmbracoAI.WorkspaceView.Test.Edit",
		name: "Test Edit Workspace View",
		element: () => import("../test-editor.element.js"),
		weight: 1000,
		meta: {
			label: "Edit",
			pathname: "edit",
			icon: "icon-edit",
		},
		conditions: [
			{
				alias: UMB_WORKSPACE_CONDITION_ALIAS,
				match: UAI_TEST_WORKSPACE_ALIAS,
			},
		],
	},
	{
		type: "workspaceAction",
		kind: "default",
		alias: "UmbracoAI.WorkspaceAction.Test.Save",
		name: "Save Test",
		api: UmbSubmitWorkspaceAction,
		meta: {
			label: "Save",
			look: "primary",
			color: "positive",
		},
		conditions: [
			{
				alias: UMB_WORKSPACE_CONDITION_ALIAS,
				match: UAI_TEST_WORKSPACE_ALIAS,
			},
		],
	},
];
