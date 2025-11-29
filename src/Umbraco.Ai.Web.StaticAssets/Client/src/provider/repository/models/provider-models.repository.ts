import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiProviderModelsServerDataSource, type UaiProviderModelsRequestArgs } from "./provider-models.server.data-source.js";
import type { UaiModelDescriptorModel } from "../../types.js";

/**
 * Repository for fetching available models from a provider.
 */
export class UaiProviderModelsRepository extends UmbControllerBase {
    #dataSource: UaiProviderModelsServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiProviderModelsServerDataSource(host);
    }

    /**
     * Requests available models for a provider, filtered by connection and capability.
     */
    async requestModels(args: UaiProviderModelsRequestArgs): Promise<{ data?: UaiModelDescriptorModel[]; error?: unknown }> {
        return this.#dataSource.getModels(args);
    }
}
