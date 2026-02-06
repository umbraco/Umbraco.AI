import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProvidersService } from "../../../api/sdk.gen.js";
import { UaiProviderTypeMapper } from "../../type-mapper.js";
import type { UaiProviderDetailModel } from "../../types.js";

/**
 * Server data source for fetching provider details.
 */
export class UaiProviderDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a provider by ID with full details including setting definitions.
     */
    async get(id: string): Promise<{ data?: UaiProviderDetailModel; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, ProvidersService.getProviderById({ path: { id } }));

        if (error || !data) {
            return { error };
        }

        return { data: UaiProviderTypeMapper.toDetailModel(data) };
    }
}
