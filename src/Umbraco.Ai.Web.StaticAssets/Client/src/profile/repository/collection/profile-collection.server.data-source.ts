import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProfilesService } from "../../../api/sdk.gen.js";
import { UaiProfileTypeMapper } from "../../type-mapper.js";
import type { UaiProfileItemModel } from "../../types.js";

/**
 * Server data source for Profile collection operations.
 */
export class UaiProfileCollectionServerDataSource implements UmbCollectionDataSource<UaiProfileItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all profiles as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            ProfilesService.getAllProfiles({
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

        const items = data.items.map(UaiProfileTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
