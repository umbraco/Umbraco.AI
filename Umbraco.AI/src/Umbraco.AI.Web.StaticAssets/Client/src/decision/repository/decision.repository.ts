import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiDecisionServerDataSource } from "./decision.server.data-source.js";
import type { UaiDecisionRequest, UaiDecisionResult } from "../types.js";

/**
 * Repository for decision operations.
 */
export class UaiDecisionRepository extends UmbControllerBase {
    #dataSource: UaiDecisionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiDecisionServerDataSource(host);
    }

    /**
     * Asks a Decision-capable AI profile a typed question.
     */
    async ask(request: UaiDecisionRequest): Promise<{ data?: UaiDecisionResult; error?: unknown }> {
        return this.#dataSource.ask(request);
    }
}
