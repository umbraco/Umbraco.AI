import { UaiConnectionConstants } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UaiConnectionConstants.Workspace.Root,
        name: "Connection Root Workspace",
        meta: {
            entityType: UaiConnectionConstants.EntityType.Root,
            headline: "Connections",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.ConnectionRoot.Collection",
        name: "Connection Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UaiConnectionConstants.Icon.Root,
            collectionAlias: UaiConnectionConstants.Collection,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UaiConnectionConstants.Workspace.Root,
            },
        ],
    },
];
