import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_TEST_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_TEST_ROOT_ENTITY_TYPE } from "../../entity.js";
import { UAI_TEST_ICON } from "../../constants.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_TEST_ROOT_WORKSPACE_ALIAS,
        name: "Tests Root Workspace",
        meta: {
            entityType: UAI_TEST_ROOT_ENTITY_TYPE,
            headline: "Tests",
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.WorkspaceView.TestsRoot.Dashboard",
        name: "Tests Dashboard Workspace View",
        element: () => import("./tests-workspace-root.element.js"),
        weight: 1000,
        meta: {
            label: "Dashboard",
            pathname: "dashboard",
            icon: UAI_TEST_ICON,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_TEST_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
