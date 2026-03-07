import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { OrchestrationsService } from "../../../api/index.js";
import { UaiOrchestrationTypeMapper } from "../../type-mapper.js";
import type { UaiOrchestrationDetailModel } from "../../types.js";
import { UAI_ORCHESTRATION_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "@umbraco-ai/core";

/**
 * Server data source for Orchestration detail operations.
 */
export class UaiOrchestrationDetailServerDataSource implements UmbDetailDataSource<UaiOrchestrationDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new orchestration.
     */
    async createScaffold(preset?: Partial<UaiOrchestrationDetailModel>) {
        const scaffold: UaiOrchestrationDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_ORCHESTRATION_ENTITY_TYPE,
            alias: "",
            name: "",
            description: null,
            profileId: null,
            surfaceIds: [],
            scope: null,
            graph: { nodes: [], edges: [] },
            isActive: true,
            dateCreated: null,
            dateModified: null,
            version: 0,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads an orchestration by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            OrchestrationsService.getOrchestrationByIdOrAlias({ path: { orchestrationIdOrAlias: unique } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiOrchestrationTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new orchestration.
     */
    async create(model: UaiOrchestrationDetailModel, _parentUnique: string | null) {
        const requestBody = UaiOrchestrationTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            OrchestrationsService.createOrchestration({ body: requestBody }),
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
     * Updates an existing orchestration.
     */
    async update(model: UaiOrchestrationDetailModel) {
        const requestBody = UaiOrchestrationTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            OrchestrationsService.updateOrchestration({
                path: { orchestrationIdOrAlias: model.unique },
                body: requestBody,
            }),
        );

        if (error) {
            return { error };
        }

        // Re-fetch to get updated version and timestamps
        return this.read(model.unique);
    }

    /**
     * Deletes an orchestration by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            OrchestrationsService.deleteOrchestration({ path: { orchestrationIdOrAlias: unique } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
