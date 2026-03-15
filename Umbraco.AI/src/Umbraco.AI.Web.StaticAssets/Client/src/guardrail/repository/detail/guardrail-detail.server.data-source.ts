import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { GuardrailsApiService } from "../../api.js";
import { UaiGuardrailTypeMapper } from "../../type-mapper.js";
import type { UaiGuardrailDetailModel } from "../../types.js";
import { UAI_GUARDRAIL_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

/**
 * Server data source for Guardrail detail operations.
 */
export class UaiGuardrailDetailServerDataSource implements UmbDetailDataSource<UaiGuardrailDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new guardrail.
     */
    async createScaffold(preset?: Partial<UaiGuardrailDetailModel>) {
        const scaffold: UaiGuardrailDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_GUARDRAIL_ENTITY_TYPE,
            alias: "",
            name: "",
            rules: [],
            dateCreated: null,
            dateModified: null,
            version: 0,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a guardrail by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            GuardrailsApiService.getGuardrailById({ path: { id: unique } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiGuardrailTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new guardrail.
     */
    async create(model: UaiGuardrailDetailModel, _parentUnique: string | null) {
        const requestBody = UaiGuardrailTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            GuardrailsApiService.createGuardrail({ body: requestBody }),
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
     * Updates an existing guardrail.
     */
    async update(model: UaiGuardrailDetailModel) {
        const requestBody = UaiGuardrailTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            GuardrailsApiService.updateGuardrail({
                path: { id: model.unique },
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
     * Deletes a guardrail by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            GuardrailsApiService.deleteGuardrail({ path: { id: unique } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
