import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { PromptsService } from "../../../api/index.js";
import { UaiPromptTypeMapper } from "../../type-mapper.js";
import type { UaiPromptItemModel } from "../../types.js";

/**
 * Server data source for Prompt collection operations.
 */
export class UaiPromptCollectionServerDataSource implements UmbCollectionDataSource<UaiPromptItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all prompts as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            PromptsService.getAllPrompts({
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

        const items = data.items.map(UaiPromptTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
