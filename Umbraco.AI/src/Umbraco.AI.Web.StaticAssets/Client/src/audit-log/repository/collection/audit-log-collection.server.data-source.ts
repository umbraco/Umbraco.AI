import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import type { UaiAuditLogItemModel } from "../../types.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AuditLogsService, UaiAuditLogTypeMapper } from "../../../app.js";

/**
 * Server data source for AuditLog collection operations.
 */
export class UaiAuditLogCollectionServerDataSource implements UmbCollectionDataSource<UaiAuditLogItemModel> {
    
     #host: UmbControllerHost; 

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all traces as collection items with filtering and pagination.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            AuditLogsService.getAuditLogs({
                query: {
                    // status: filter.filter?.status,
                    // userId: filter.filter?.userId,
                    // profileId: filter.filter?.profileId,
                    // providerId: filter.filter?.providerId,
                    // entityId: filter.filter?.entityId,
                    // fromDate: filter.filter?.fromDate,
                    // toDate: filter.filter?.toDate,
                    searchText: filter.filter,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiAuditLogTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
