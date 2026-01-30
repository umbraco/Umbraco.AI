
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_AUDIT_LOG_COLLECTION_ALIAS, UAI_AUDIT_LOG_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_AUDIT_LOG_ROOT_ENTITY_TYPE } from "../../entity.js";
import { UAI_AUDIT_LOG_ICON } from "../../collection/constants.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_AUDIT_LOG_ROOT_WORKSPACE_ALIAS,
        name: "Audit Log Root Workspace",
        meta: {
            entityType: UAI_AUDIT_LOG_ROOT_ENTITY_TYPE,
            headline: "Logs",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.AuditLogRoot.Collection",
        name: "Audit Log Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_AUDIT_LOG_ICON,
            collectionAlias: UAI_AUDIT_LOG_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AUDIT_LOG_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
