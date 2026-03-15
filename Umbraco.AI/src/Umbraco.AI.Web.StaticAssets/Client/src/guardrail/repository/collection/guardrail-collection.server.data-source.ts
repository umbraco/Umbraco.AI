import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbCollectionDataSource, UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardrailsApiService } from "../../api.js";
import { UaiGuardrailTypeMapper } from "../../type-mapper.js";
import type { UaiGuardrailItemModel } from "../../types.js";

/**
 * Server data source for Guardrail collection operations.
 */
export class UaiGuardrailCollectionServerDataSource implements UmbCollectionDataSource<UaiGuardrailItemModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all guardrails as collection items.
     */
    async getCollection(filter: UmbCollectionFilterModel) {
        const { data, error } = await tryExecute(
            this.#host,
            GuardrailsApiService.getAllGuardrails({
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

        const items = data.items.map(UaiGuardrailTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }
}
