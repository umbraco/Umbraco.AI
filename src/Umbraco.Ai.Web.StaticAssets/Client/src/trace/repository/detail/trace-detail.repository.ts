import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiTraceDetailServerDataSource } from "./trace-detail.server.data-source.js";
import { UAI_TRACE_DETAIL_STORE_CONTEXT } from "./trace-detail.store.js";
import type { UaiTraceDetailModel } from "../../types.js";
import { UAI_TRACE_ROOT_ENTITY_TYPE } from "../../entity.js";

/**
 * Repository for Trace detail operations (read/delete only - traces are system-generated).
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiTraceDetailRepository extends UmbDetailRepositoryBase<UaiTraceDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiTraceDetailServerDataSource, UAI_TRACE_DETAIL_STORE_CONTEXT);
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            // Notify that a trace was deleted so collections can refresh
            this.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_TRACE_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }
}

export { UaiTraceDetailRepository as api };
