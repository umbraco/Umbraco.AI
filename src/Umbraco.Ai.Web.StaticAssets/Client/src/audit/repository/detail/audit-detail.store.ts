import { UmbDetailStoreBase } from "@umbraco-cms/backoffice/store";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiAuditDetailModel } from "../../types.js";

export const UAI_AUDIT_DETAIL_STORE_CONTEXT = new UmbContextToken<UaiAuditDetailStore>(
    "UaiAuditDetailStore"
);

/**
 * Store for Audit detail data.
 * Extends the CMS detail store base for consistent caching behavior.
 */
export class UaiAuditDetailStore extends UmbDetailStoreBase<UaiAuditDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UAI_AUDIT_DETAIL_STORE_CONTEXT.toString());
    }
}

export { UaiAuditDetailStore as api };
