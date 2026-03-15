import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardrailsService } from "../../../api/sdk.gen.js";
import type { GuardrailEvaluatorInfoModel } from "../../../api/types.gen.js";

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
    async getItems(): Promise<{ data?: GuardrailEvaluatorInfoModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, GuardrailsService.getAllGuardrailEvaluators());

        if (error || !data) {
            return { error };
        }

        return { data };
    }
}
