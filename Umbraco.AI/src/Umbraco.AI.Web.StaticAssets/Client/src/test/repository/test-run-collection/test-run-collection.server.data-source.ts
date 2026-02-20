import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import { UaiTestTypeMapper } from "../../type-mapper.js";
import type { UaiTestRunItemModel, UaiTestRunCollectionFilterModel } from "../../types.js";

/**
 * Server data source for Test Run collection operations.
 * Accepts an optional test scope filter to return runs for a specific test.
 */
export class UaiTestRunCollectionServerDataSource implements UmbCollectionDataSource<UaiTestRunItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getCollection(filter: UaiTestRunCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getAllTestRuns({
                query: {
                    testId: filter.test?.unique,
                    status: filter.filter || undefined,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            }),
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UaiTestTypeMapper.toRunItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
