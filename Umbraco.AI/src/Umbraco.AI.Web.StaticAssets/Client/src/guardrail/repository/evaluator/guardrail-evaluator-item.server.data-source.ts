import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardrailsApiService } from "../../api.js";
import type { GuardrailEvaluatorInfoApiModel } from "../../api.js";

/**
 * Server data source for fetching guardrail evaluator items.
 */
export class UaiGuardrailEvaluatorItemServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches all available guardrail evaluators.
     */
    async getItems(): Promise<{ data?: GuardrailEvaluatorInfoApiModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, GuardrailsApiService.getAllGuardrailEvaluators());

        if (error || !data) {
            return { error };
        }

        return { data };
    }
}
