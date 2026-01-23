import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_SETTINGS_ROOT_WORKSPACE_ALIAS, UAI_SETTINGS_ICON } from "../../constants.js";
import { UAI_SETTINGS_ROOT_ENTITY_TYPE } from "../../entity.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_SETTINGS_ROOT_WORKSPACE_ALIAS,
        name: "Settings Root Workspace",
        meta: {
            entityType: UAI_SETTINGS_ROOT_ENTITY_TYPE,
            headline: "Settings",
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.WorkspaceView.SettingsRoot.Editor",
        name: "Settings Editor Workspace View",
        element: () => import('./settings-editor.element.js'),
        weight: 1000,
        meta: {
            label: "Settings",
            pathname: "settings",
            icon: UAI_SETTINGS_ICON,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_SETTINGS_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
