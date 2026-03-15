import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiGuardrailEvaluatorItemServerDataSource } from "./guardrail-evaluator-item.server.data-source.js";
import type { GuardrailEvaluatorInfoApiModel } from "../../api.js";

/**
 * Repository for fetching guardrail evaluator items.
 */
export class UaiGuardrailEvaluatorItemRepository extends UmbControllerBase {
    #dataSource: UaiGuardrailEvaluatorItemServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiGuardrailEvaluatorItemServerDataSource(host);
    }

    /**
     * Requests all available guardrail evaluators.
     */
    async requestItems(): Promise<{ data?: GuardrailEvaluatorInfoApiModel[]; error?: unknown }> {
        return this.#dataSource.getItems();
    }
}

export { UaiGuardrailEvaluatorItemRepository as api };
