import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import { UaiConnectionTypeMapper } from "../../type-mapper.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UaiConnectionConstants } from "../../constants.js";

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
            unique: "",
            entityType: UaiConnectionConstants.EntityType.Entity,
            alias: "",
            name: "",
            providerId: preset?.providerId ?? "",
            settings: null,
            isActive: true,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a connection by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.getConnectionsById({ path: { id: unique } })
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

        const { response, error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.postConnections({ body: requestBody })
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

        const { error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.putConnectionsById({
                path: { id: model.unique },
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
        const { error } = await tryExecuteAndNotify(
            this.#host,
            ConnectionsService.deleteConnectionsById({ path: { id: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
