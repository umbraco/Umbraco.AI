import {
    UAI_AUDIT_DETAIL_REPOSITORY_ALIAS,
    UAI_AUDIT_DETAIL_STORE_ALIAS,
    UAI_AUDIT_COLLECTION_REPOSITORY_ALIAS,
} from "../constants.js";

export const auditRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: UAI_AUDIT_DETAIL_REPOSITORY_ALIAS,
        name: "Audit Detail Repository",
        api: () => import("./detail/audit-detail.repository.ts"),
    },
    {
        type: "store",
        alias: UAI_AUDIT_DETAIL_STORE_ALIAS,
        name: "Audit Detail Store",
        api: () => import("./detail/audit-detail.store.ts"),
    },
    {
        type: "repository",
        alias: UAI_AUDIT_COLLECTION_REPOSITORY_ALIAS,
        name: "Audit Collection Repository",
        api: () => import("./collection/audit-collection.repository.ts"),
    },
];
