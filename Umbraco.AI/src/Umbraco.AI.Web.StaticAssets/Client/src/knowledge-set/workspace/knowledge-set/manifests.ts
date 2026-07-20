import { UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS } from "../constants.js";
import { UAI_KNOWLEDGE_SET_ENTITY_TYPE } from "../../entity.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

/**
 * Read-only per-set workspace, mirroring the Context workspace: a routable workspace with a Details
 * view (item card grid) and an Info view (id/metadata).
 *
 * Unlike the Context workspace there is no save action, no entity actions and no property editors — the
 * views render the set's metadata and items for auditing only.
 */
export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS,
        name: "Knowledge Set Workspace",
        api: () => import("./knowledge-set-workspace.context.js"),
        meta: {
            entityType: UAI_KNOWLEDGE_SET_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.KnowledgeSet.View.Details",
        name: "Knowledge Set Details Workspace View",
        js: () => import("./views/knowledge-set-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-book",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.KnowledgeSet.View.Info",
        name: "Knowledge Set Info Workspace View",
        js: () => import("./views/knowledge-set-info-workspace-view.element.js"),
        weight: 90,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS,
            },
        ],
    },
];
