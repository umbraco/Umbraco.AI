import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_TEST_WORKSPACE_ALIAS, UAI_TEST_ENTITY_TYPE, UAI_TEST_RUN_COLLECTION_ALIAS, UAI_TEST_RUN_ICON } from "../../constants.js";
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
		alias: "UmbracoAI.Workspace.Test.View.Details",
		name: "Test Details Workspace View",
		js: () => import("./views/test-details-workspace-view.element.js"),
		weight: 300,
		meta: {
			label: "Settings",
			pathname: "settings",
			icon: "icon-settings",
		},
		conditions: [
			{
				alias: UMB_WORKSPACE_CONDITION_ALIAS,
				match: UAI_TEST_WORKSPACE_ALIAS,
			},
		],
	},
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Test.View.Grading",
        name: "Test Grading Workspace View",
        js: () => import("./views/test-grading-workspace-view.element.js"),
        weight: 200,
        meta: {
            label: "Grading",
            pathname: "grading",
            icon: "icon-equalizer",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_TEST_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAI.Workspace.Test.View.Runs",
        name: "Test Runs Workspace View",
        element: () => import("./views/test-runs-workspace-view.element.js"),
        weight: 150,
        meta: {
            label: "Runs",
            pathname: "runs",
            icon: UAI_TEST_RUN_ICON,
            collectionAlias: UAI_TEST_RUN_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_TEST_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Test.View.Info",
        name: "Test Info Workspace View",
        js: () => import("./views/test-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
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
