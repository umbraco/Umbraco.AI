import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiPromptRegistrarServerDataSource } from "./prompt-registrar.server.data-source.js";
import type { UaiPromptRegistrationModel } from "../../property-actions/types.js";

/**
 * Repository for fetching prompts for property action registration.
 */
export class UaiPromptRegistrarRepository extends UmbRepositoryBase {
    #dataSource: UaiPromptRegistrarServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiPromptRegistrarServerDataSource(host);
    }

    /**
     * Gets all active prompts for registration as property actions.
     */
    async getActivePrompts(): Promise<{ data?: UaiPromptRegistrationModel[]; error?: unknown }> {
        return this.#dataSource.getActivePrompts();
    }
}
