import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
// import { tryExecute } from "@umbraco-cms/backoffice/resources";  // TODO: Uncomment when API is available
// TODO: Replace with actual import once API is regenerated
// import { AuditService } from "../../../api/sdk.gen.js";
// import { UaiAuditTypeMapper } from "../../type-mapper.js";  // TODO: Uncomment when API is available
import type { UaiAuditItemModel } from "../../types.js";

/**
 * Server data source for Audit collection operations.
 */
export class UaiAuditCollectionServerDataSource implements UmbCollectionDataSource<UaiAuditItemModel> {
    // #host: UmbControllerHost;  // TODO: Uncomment when API is available

    constructor(_host: UmbControllerHost) {
        // this.#host = host;  // TODO: Uncomment when API is available
    }

    /**
     * Gets all traces as collection items with filtering and pagination.
     */
    async getCollection(_filter: UmbCollectionFilterModel) {
        // TODO: Uncomment once AuditService is available in generated API
        /*
        const { data, error } = await tryExecute(
            this.#host,
            AuditService.getAllAudits({
                query: {
                    status: filter.filter?.status,
                    userId: filter.filter?.userId,
                    profileId: filter.filter?.profileId,
                    providerId: filter.filter?.providerId,
                    entityId: filter.filter?.entityId,
                    fromDate: filter.filter?.fromDate,
                    toDate: filter.filter?.toDate,
                    searchText: filter.filter?.searchText,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiAuditTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
        */

        // Temporary mock data until API is available
        console.warn('AuditService not yet available - using mock data');
        return {
            data: {
                items: [],
                total: 0,
            },
        };
    }
}
