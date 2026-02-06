import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ContextsService } from "../../../api/sdk.gen.js";
import { UaiContextTypeMapper } from "../../type-mapper.js";
import type { UaiContextItemModel } from "../../types.js";

/**
 * Server data source for Context collection operations.
 */
export class UaiContextCollectionServerDataSource implements UmbCollectionDataSource<UaiContextItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all contexts as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            ContextsService.getAllContexts({
                query: {
                    filter: filter.filter,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            }),
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiContextTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
