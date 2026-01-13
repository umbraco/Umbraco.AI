import {
    UAI_AUDIT_LOG_DETAIL_REPOSITORY_ALIAS,
    UAI_AUDIT_LOG_DETAIL_STORE_ALIAS,
    UAI_AUDIT_LOG_COLLECTION_REPOSITORY_ALIAS,
} from "../constants.js";

export const auditLogRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: UAI_AUDIT_LOG_DETAIL_REPOSITORY_ALIAS,
        name: "AuditLog Detail Repository",
        api: () => import("./detail/audit-detail.repository.ts"),
    },
    {
        type: "store",
        alias: UAI_AUDIT_LOG_DETAIL_STORE_ALIAS,
        name: "AuditLog Detail Store",
        api: () => import("./detail/audit-detail.store.ts"),
    },
    {
        type: "repository",
        alias: UAI_AUDIT_LOG_COLLECTION_REPOSITORY_ALIAS,
        name: "AuditLog Collection Repository",
        api: () => import("./collection/audit-collection.repository.ts"),
    },
];
