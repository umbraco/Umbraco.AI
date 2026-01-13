import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiAuditDetailServerDataSource } from "./audit-detail.server.data-source.ts";
import { UAI_AUDIT_DETAIL_STORE_CONTEXT } from "./audit-detail.store.ts";
import type { UaiAuditDetailModel } from "../../types.js";
import { UAI_AUDIT_ROOT_ENTITY_TYPE } from "../../entity.js";

/**
 * Repository for Audit detail operations (read/delete only - traces are system-generated).
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiAuditDetailRepository extends UmbDetailRepositoryBase<UaiAuditDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiAuditDetailServerDataSource, UAI_AUDIT_DETAIL_STORE_CONTEXT);
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            // Notify that a trace was deleted so collections can refresh
            this.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_AUDIT_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }
}

export { UaiAuditDetailRepository as api };
