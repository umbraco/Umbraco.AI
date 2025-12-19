import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAiAgentRegistrarServerDataSource } from "./prompt-registrar.server.data-source.js";
import type { UAiAgentRegistrationModel } from "../../property-actions/types.js";

/**
 * Repository for fetching Agents for property action registration.
 */
export class UAiAgentRegistrarRepository extends UmbRepositoryBase {
    #dataSource: UAiAgentRegistrarServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UAiAgentRegistrarServerDataSource(host);
    }

    /**
     * Gets all active Agents for registration as property actions.
     */
    async getActiveAgents(): Promise<{ data?: UAiAgentRegistrationModel[]; error?: unknown }> {
        return this.#dataSource.getActiveAgents();
    }
}
