import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROFILE_WORKSPACE_ALIAS, UAI_PROFILE_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROFILE_CAPABILITY_CONDITION_ALIAS } from "./profile-capability.condition.js";

export const manifests: Array<UmbExtensionManifest> = [
    // Workspace condition: matches profile capability from workspace context
    {
        type: "condition",
        alias: UAI_PROFILE_CAPABILITY_CONDITION_ALIAS,
        name: "Profile Capability Condition",
        api: () => import("./profile-capability.condition.js"),
    },
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
        alias: "UmbracoAI.Workspace.Profile.View.Details",
        name: "Profile Details Workspace View",
        js: () => import("./views/profile-details-workspace-view.element.js"),
        weight: 300,
        meta: {
            label: "Settings",
            pathname: "settings",
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
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Profile.View.Governance",
        name: "Profile Governance Workspace View",
        js: () => import("./views/profile-governance-workspace-view.element.js"),
        weight: 200,
        meta: {
            label: "Governance",
            pathname: "governance",
            icon: "icon-shield",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROFILE_WORKSPACE_ALIAS,
            },
            {
                alias: UAI_PROFILE_CAPABILITY_CONDITION_ALIAS,
                match: "chat",
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Profile.View.Info",
        name: "Profile Info Workspace View",
        js: () => import("./views/profile-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
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
        alias: "UmbracoAI.WorkspaceAction.Profile.Save",
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
