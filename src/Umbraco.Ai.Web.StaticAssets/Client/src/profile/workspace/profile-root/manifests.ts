import {
    UAI_PROFILE_ROOT_WORKSPACE_ALIAS,
    UAI_PROFILE_ROOT_ENTITY_TYPE,
    UAI_PROFILE_ICON,
} from "../../constants.js";
import { UAI_PROFILE_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_PROFILE_ROOT_WORKSPACE_ALIAS,
        name: "Profile Root Workspace",
        meta: {
            entityType: UAI_PROFILE_ROOT_ENTITY_TYPE,
            headline: "Profiles",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.ProfileRoot.Collection",
        name: "Profile Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_PROFILE_ICON,
            collectionAlias: UAI_PROFILE_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROFILE_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
