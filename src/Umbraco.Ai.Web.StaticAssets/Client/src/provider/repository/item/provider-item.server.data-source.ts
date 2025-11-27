import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ProvidersService } from "../../../api/sdk.gen.js";
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
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ProvidersService.getProviders()
        );

        if (error || !data) {
            return { error };
        }

        // Map API response to UI model
        const items: UaiProviderItemModel[] = data.map((provider) => ({
            id: provider.id,
            name: provider.name,
            capabilities: provider.capabilities,
        }));

        return { data: items };
    }
}
