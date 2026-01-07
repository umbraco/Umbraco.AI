import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProvidersService } from "../../../api/sdk.gen.js";
import { UaiProviderTypeMapper } from "../../type-mapper.js";
import type { UaiProviderItemModel } from "../../types.js";

/**
 * Server data source for fetching provider items.
 */
export class UaiProviderItemServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches all available providers.
     */
    async getItems(): Promise<{ data?: UaiProviderItemModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ProvidersService.getAllProviders()
        );

        if (error || !data) {
            return { error };
        }

        const items = data.map(UaiProviderTypeMapper.toItemModel);

        return { data: items };
    }
}
