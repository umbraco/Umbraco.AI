import { UAI_AUDIT_COLLECTION_ALIAS, UAI_AUDIT_COLLECTION_REPOSITORY_ALIAS, UAI_AUDIT_TABLE_VIEW_ALIAS } from "../constants.js";

export const auditCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_AUDIT_COLLECTION_ALIAS,
        name: "Audit Collection",
        element: () => import("./audit-collection.element.ts"),
        meta: {
            repositoryAlias: UAI_AUDIT_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: UAI_AUDIT_TABLE_VIEW_ALIAS,
        name: "Audit Table View",
        element: () => import("./views/table/audit-table-collection-view.element.ts"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_AUDIT_COLLECTION_ALIAS }],
    },
];
