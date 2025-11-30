import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiModelDescriptorModel } from "../../types.js";

export interface UaiConnectionModelsRequestArgs {
    connectionId: string;
    capability?: string;
}

/**
 * Server data source for fetching available models from a connection.
 */
export class UaiConnectionModelsServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches available models for a connection, optionally filtered by capability.
     */
    async getModels(args: UaiConnectionModelsRequestArgs): Promise<{ data?: UaiModelDescriptorModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getModels({
                path: { connectionIdOrAlias: args.connectionId },
                query: { capability: args.capability },
            })
        );

        if (error || !data) {
            return { error };
        }

        return { data: data.map(UaiConnectionTypeMapper.toModelDescriptorModel) };
    }
}
