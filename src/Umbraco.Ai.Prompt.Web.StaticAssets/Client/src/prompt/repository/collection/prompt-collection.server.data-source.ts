import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UaiPromptItemModel } from "../../types.js";
import { UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";

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
        const skip = filter.skip ?? 0;
        const take = filter.take ?? 100;

        const { data, error } = await tryExecute(
            this.#host,
            fetch(`/umbraco/ai/management/api/v1/prompts?skip=${skip}&take=${take}`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' },
            }).then(res => res.ok ? res.json() : Promise.reject(res))
        );

        if (error || !data) {
            return { error };
        }

        const items = (data.items as Array<Record<string, unknown>>).map((item): UaiPromptItemModel => ({
            unique: item.id as string,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: item.alias as string,
            name: item.name as string,
            description: (item.description as string) ?? null,
            isActive: item.isActive as boolean,
        }));

        return {
            data: {
                items,
                total: data.total as number,
            },
        };
    }
}
