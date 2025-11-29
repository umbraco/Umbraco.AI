import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROFILE_WORKSPACE_ALIAS, UAI_PROFILE_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_PROFILE_WORKSPACE_ALIAS,
        name: "Profile Workspace",
        api: () => import("./profile-workspace.context.js"),
        meta: {
            entityType: UAI_PROFILE_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.Workspace.Profile.View.Details",
        name: "Profile Details Workspace View",
        js: () => import("./views/profile-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROFILE_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAi.WorkspaceAction.Profile.Save",
        name: "Save Profile",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROFILE_WORKSPACE_ALIAS,
            },
        ],
    },
];
