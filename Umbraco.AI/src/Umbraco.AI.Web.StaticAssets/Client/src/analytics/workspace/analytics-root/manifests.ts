import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_ANALYTICS_ROOT_ENTITY_TYPE } from "../../entity.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS,
        name: "Analytics Root Workspace",
        element: () => import("./analytics-workspace.element.js"),
        api: () => import("./analytics-workspace.context.js"),
        meta: {
            entityType: UAI_ANALYTICS_ROOT_ENTITY_TYPE,
            headline: "#uaiAnalytics_title",
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.WorkspaceView.AnalyticsRoot.Dashboard",
        name: "Analytics Dashboard Workspace View",
        element: () => import("./analytics-dashboard.element.js"),
        weight: 1000,
        meta: {
            label: "Usage",
            pathname: "usage",
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
