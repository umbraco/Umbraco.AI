import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProvidersService } from "../../../api/sdk.gen.js";
import { UaiProviderTypeMapper } from "../../type-mapper.js";
import type { UaiModelDescriptorModel } from "../../types.js";

export interface UaiProviderModelsRequestArgs {
    providerId: string;
    connectionId: string;
    capability: string;
}

/**
 * Server data source for fetching available models from a provider.
 */
export class UaiProviderModelsServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches available models for a provider, filtered by connection and capability.
     */
    async getModels(args: UaiProviderModelsRequestArgs): Promise<{ data?: UaiModelDescriptorModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ProvidersService.getModelsByProviderId({
                path: { id: args.providerId },
                query: { connectionId: args.connectionId, capability: args.capability },
            })
        );

        if (error || !data) {
            return { error };
        }

        return { data: data.map(UaiProviderTypeMapper.toModelDescriptorModel) };
    }
}
