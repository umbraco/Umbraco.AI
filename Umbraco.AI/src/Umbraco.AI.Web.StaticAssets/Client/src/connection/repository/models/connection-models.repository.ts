import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiConnectionModelsServerDataSource, type UaiConnectionModelsRequestArgs } from "./connection-models.server.data-source.js";
import type { UaiModelDescriptorModel } from "../../types.js";

/**
 * Repository for fetching available models from a connection.
 */
export class UaiConnectionModelsRepository extends UmbControllerBase {
    #dataSource: UaiConnectionModelsServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiConnectionModelsServerDataSource(host);
    }

    /**
     * Requests available models for a connection, optionally filtered by capability.
     */
    async requestModels(args: UaiConnectionModelsRequestArgs): Promise<{ data?: UaiModelDescriptorModel[]; error?: unknown }> {
        return this.#dataSource.getModels(args);
    }
}
