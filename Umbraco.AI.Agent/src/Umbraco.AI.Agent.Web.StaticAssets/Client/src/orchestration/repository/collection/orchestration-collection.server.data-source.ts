import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { OrchestrationsService } from "../../../api/index.js";
import { UaiOrchestrationTypeMapper } from "../../type-mapper.js";
import type { UaiOrchestrationItemModel } from "../../types.js";

/**
 * Server data source for Orchestration collection operations.
 */
export class UaiOrchestrationCollectionServerDataSource
    implements UmbCollectionDataSource<UaiOrchestrationItemModel>
{
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all Orchestrations as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            OrchestrationsService.getAllOrchestrations({
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

        const items = data.items.map(UaiOrchestrationTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
