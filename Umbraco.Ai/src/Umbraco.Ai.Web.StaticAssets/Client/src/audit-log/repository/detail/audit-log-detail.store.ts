import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiAuditLogDetailModel } from "../../types.js";

export const UAI_AUDIT_LOG_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiAuditLogDetailStore>(
    "UaiAuditLogDetailStore"
);

/**
 * Store for AuditLog detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiAuditLogDetailStore extends UmbDetailStoreBase<UaiAuditLogDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_AUDIT_LOG_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiAuditLogDetailStore as api };
