import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiAuditLogDetailsModalData {
    uniques?: string[];
}

export interface UaiAuditLogDetailsModalValue {
    unique?: string;
}

export const UAI_AUDIT_LOG_DETAILS_MODAL = new UmbModalToken<
    UaiAuditLogDetailsModalData,
    UaiAuditLogDetailsModalValue
>("UmbracoAI.Modal.AuditLog.Details", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});
