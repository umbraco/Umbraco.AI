import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionItemModel } from "../../types.js";

/**
 * Server data source for Connection collection operations.
 */
export class UaiConnectionCollectionServerDataSource implements UmbCollectionDataSource<UaiConnectionItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all connections as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.getConnections({
                query: {
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiConnectionTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
