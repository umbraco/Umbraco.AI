import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

/**
 * Server data source for Connection detail operations.
 */
export class UaiConnectionDetailServerDataSource implements UmbDetailDataSource<UaiConnectionDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new connection.
     */
    async createScaffold(preset?: Partial<UaiConnectionDetailModel>) {
        const scaffold: UaiConnectionDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_CONNECTION_ENTITY_TYPE,
            alias: "",
            name: "",
            providerId: preset?.providerId ?? "",
            settings: null,
            isActive: true,
            dateCreated: null,
            dateModified: null,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a connection by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getConnectionByIdOrAlias({ path: { connectionIdOrAlias: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiConnectionTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new connection.
     */
    async create(model: UaiConnectionDetailModel, _parentUnique: string | null) {
        const requestBody = UaiConnectionTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            ConnectionsService.createConnection({ body: requestBody })
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
     * Updates an existing connection.
     */
    async update(model: UaiConnectionDetailModel) {
        const requestBody = UaiConnectionTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            ConnectionsService.updateConnection({
                path: { connectionIdOrAlias: model.unique },
                body: requestBody,
            })
        );

        if (error) {
            return { error };
        }

        return { data: model };
    }

    /**
     * Deletes a connection by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            ConnectionsService.deleteConnection({ path: { connectionIdOrAlias: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
