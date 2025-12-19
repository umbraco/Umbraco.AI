import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";
import { UAiAgentTypeMapper } from "../../type-mapper.js";
import type { UAiAgentItemModel } from "../../types.js";

/**
 * Server data source for Agent collection operations.
 */
export class UAiAgentCollectionServerDataSource implements UmbCollectionDataSource<UAiAgentItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all Agents as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            AgentsService.getAllAgents({
                query: {
                    filter: filter.filter,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 100,
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        const items = data.items.map(UAiAgentTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
