import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { KnowledgeSetsService } from "../../../api/sdk.gen.js";
import { UaiKnowledgeSetTypeMapper } from "../../type-mapper.js";
import type { UaiKnowledgeSetItemModel } from "../../types.js";

/**
 * Server data source for Knowledge Set collection operations.
 */
export class UaiKnowledgeSetCollectionServerDataSource implements UmbCollectionDataSource<UaiKnowledgeSetItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all installed knowledge sets as collection items.
     *
     * Knowledge sets are code-defined and auto-active — there is no create/update/delete. The endpoint
     * returns the full (typically small) set of installed sets every time, so filtering is applied
     * client-side.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(this.#host, KnowledgeSetsService.getAllKnowledgeSets());

        if (error || !data) {
            return { error };
        }

        let items = data.map(UaiKnowledgeSetTypeMapper.toItemModel);

        if (filter.filter) {
            const search = filter.filter.toLowerCase();
            items = items.filter(
                (item) =>
                    item.name.toLowerCase().includes(search) || (item.description?.toLowerCase().includes(search) ?? false),
            );
        }

        return {
            data: {
                items,
                total: items.length,
            },
        };
    }
}
