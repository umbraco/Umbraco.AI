import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";
import { UaiAgentTypeMapper } from "../../type-mapper.js";
import type { UaiAgentDetailModel } from "../../types.js";
import { UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "@umbraco-ai/core";

/**
 * Server data source for Agent detail operations.
 */
export class UaiAgentDetailServerDataSource implements UmbDetailDataSource<UaiAgentDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new agent.
     */
    async createScaffold(preset?: Partial<UaiAgentDetailModel>) {
        const scaffold: UaiAgentDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: "",
            name: "",
            description: null,
            profileId: null,
            contextIds: [],
            instructions: null,
            isActive: true,
            dateCreated: null,
            dateModified: null,
            version: 0,
            scopeIds: [],
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads an agent by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            AgentsService.getAgentByIdOrAlias({ path: { agentIdOrAlias: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiAgentTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new agent.
     */
    async create(model: UaiAgentDetailModel, _parentUnique: string | null) {
        const requestBody = UaiAgentTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            AgentsService.createAgent({ body: requestBody })
        );

        if (error) {
            return { error };
        }

        // Extract the ID from the Location header
        const locationHeader = response?.headers?.get("Location") ?? "";
        const unique = locationHeader.split("/").pop() ?? "";

        return {
            data: {
                ...model,
                unique,
            },
        };
    }

    /**
     * Updates an existing agent.
     */
    async update(model: UaiAgentDetailModel) {
        const requestBody = UaiAgentTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            AgentsService.updateAgent({
                path: { agentIdOrAlias: model.unique },
                body: requestBody,
            })
        );

        if (error) {
            return { error };
        }

        // Re-fetch to get updated version and timestamps
        return this.read(model.unique);
    }

    /**
     * Deletes an agent by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            AgentsService.deleteAgent({ path: { agentIdOrAlias: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
