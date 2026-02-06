import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_ANALYTICS_ROOT_ENTITY_TYPE } from "../../entity.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS,
        name: "Analytics Root Workspace",
        meta: {
            entityType: UAI_ANALYTICS_ROOT_ENTITY_TYPE,
            headline: "Analytics",
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.WorkspaceView.AnalyticsRoot.Dashboard",
        name: "Analytics Dashboard Workspace View",
        element: () => import("./analytics-dashboard.element.js"),
        weight: 1000,
        meta: {
            label: "Dashboard",
            pathname: "dashboard",
            icon: "icon-chart",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
