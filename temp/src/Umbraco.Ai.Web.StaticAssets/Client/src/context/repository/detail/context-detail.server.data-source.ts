import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ContextsService } from "../../../api/sdk.gen.js";
import { UaiContextTypeMapper } from "../../type-mapper.js";
import type { UaiContextDetailModel } from "../../types.js";
import { UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

/**
 * Server data source for Context detail operations.
 */
export class UaiContextDetailServerDataSource implements UmbDetailDataSource<UaiContextDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new context.
     */
    async createScaffold(preset?: Partial<UaiContextDetailModel>) {
        const scaffold: UaiContextDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_CONTEXT_ENTITY_TYPE,
            alias: "",
            name: "",
            resources: [],
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a context by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            ContextsService.getContextByIdOrAlias({ path: { contextIdOrAlias: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiContextTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new context.
     */
    async create(model: UaiContextDetailModel, _parentUnique: string | null) {
        const requestBody = UaiContextTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            ContextsService.createContext({ body: requestBody })
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
     * Updates an existing context.
     */
    async update(model: UaiContextDetailModel) {
        const requestBody = UaiContextTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            ContextsService.updateContext({
                path: { contextIdOrAlias: model.unique },
                body: requestBody,
            })
        );

        if (error) {
            return { error };
        }

        return { data: model };
    }

    /**
     * Deletes a context by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            ContextsService.deleteContext({ path: { contextIdOrAlias: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
