import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const auditLogDetailsModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "UmbracoAi.Modal.AuditLog.Details",
        name: "Audit Log Details Modal",
        element: () => import("./audit-log-details/audit-log-details-modal.element.js"),
    },
];
