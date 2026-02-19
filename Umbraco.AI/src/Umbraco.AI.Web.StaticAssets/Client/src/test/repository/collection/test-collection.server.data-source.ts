import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import { UaiTestTypeMapper } from "../../type-mapper.js";
import type { UaiTestItemModel } from "../../types.js";

/**
 * Server data source for Test collection operations.
 */
export class UaiTestCollectionServerDataSource implements UmbCollectionDataSource<UaiTestItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all tests as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getAllTests({
                query: {
                    filter: filter.filter,
                    tags: undefined, // Can be extended to support tag filtering
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            }),
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiTestTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
