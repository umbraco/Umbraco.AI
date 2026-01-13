import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiAuditLogDetailServerDataSource } from "./audit-detail.server.data-source.ts";
import { UAI_AUDIT_LOG_DETAIL_STORE_CONTEXT } from "./audit-detail.store.ts";
import type { UaiAuditLogDetailModel } from "../../types.js";
import { UAI_AUDIT_LOG_ROOT_ENTITY_TYPE } from "../../entity.js";

/**
 * Repository for AuditLog detail operations (read/delete only - traces are system-generated).
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiAuditLogDetailRepository extends UmbDetailRepositoryBase<UaiAuditLogDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiAuditLogDetailServerDataSource, UAI_AUDIT_LOG_DETAIL_STORE_CONTEXT);
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            // Notify that a trace was deleted so collections can refresh
            this.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_AUDIT_LOG_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }
}

export { UaiAuditLogDetailRepository as api };
