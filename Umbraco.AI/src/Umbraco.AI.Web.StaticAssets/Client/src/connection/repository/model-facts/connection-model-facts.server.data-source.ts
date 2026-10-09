import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiModelFactsModel } from "../../types.js";

export interface UaiConnectionModelFactsRequestArgs {
    connectionId: string;
    capability: string;
    modelId?: string;
}

/**
 * Server data source for fetching model facts (context window, price, ...) from a connection.
 */
export class UaiConnectionModelFactsServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches model facts for a connection and capability, optionally narrowed to a single model.
     */
    async getModelFacts(
        args: UaiConnectionModelFactsRequestArgs,
    ): Promise<{ data?: UaiModelFactsModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getModelFacts({
                path: { connectionIdOrAlias: args.connectionId },
                query: { capability: args.capability, modelId: args.modelId },
            }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: data.items.map(UaiConnectionTypeMapper.toModelFactsModel) };
    }
}
