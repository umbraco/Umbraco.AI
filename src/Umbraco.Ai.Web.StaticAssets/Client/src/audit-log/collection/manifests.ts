import { UAI_AUDIT_LOG_COLLECTION_ALIAS, UAI_AUDIT_LOG_COLLECTION_REPOSITORY_ALIAS, UAI_AUDIT_LOG_TABLE_VIEW_ALIAS } from "../constants.js";

export const auditLogCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_AUDIT_LOG_COLLECTION_ALIAS,
        name: "AuditLog Collection",
        element: () => import("./audit-collection.element.ts"),
        meta: {
            repositoryAlias: UAI_AUDIT_LOG_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: UAI_AUDIT_LOG_TABLE_VIEW_ALIAS,
        name: "AuditLog Table View",
        element: () => import("./views/table/audit-table-collection-view.element.ts"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_AUDIT_LOG_COLLECTION_ALIAS }],
    },
];
