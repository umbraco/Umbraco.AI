import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import {
    UaiConnectionModelFactsServerDataSource,
    type UaiConnectionModelFactsRequestArgs,
} from "./connection-model-facts.server.data-source.js";
import type { UaiModelFactsModel } from "../../types.js";

/**
 * Repository for fetching model facts (context window, price, ...) from a connection.
 */
export class UaiConnectionModelFactsRepository extends UmbControllerBase {
    #dataSource: UaiConnectionModelFactsServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiConnectionModelFactsServerDataSource(host);
    }

    /**
     * Requests model facts for a connection and capability, optionally narrowed to a single model.
     */
    async requestModelFacts(
        args: UaiConnectionModelFactsRequestArgs,
    ): Promise<{ data?: UaiModelFactsModel[]; error?: unknown }> {
        return this.#dataSource.getModelFacts(args);
    }
}
