import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from "@umbraco-cms/backoffice/resources";
import { ProfilesService } from "../../../api/sdk.gen.js";
import { UaiProfileTypeMapper } from "../../type-mapper.js";
import type { UaiProfileDetailModel } from "../../types.js";
import { UAI_PROFILE_ENTITY_TYPE } from "../../constants.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

/**
 * Server data source for Profile detail operations.
 */
export class UaiProfileDetailServerDataSource implements UmbDetailDataSource<UaiProfileDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new profile.
     */
    async createScaffold(preset?: Partial<UaiProfileDetailModel>) {
        const scaffold: UaiProfileDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_PROFILE_ENTITY_TYPE,
            alias: "",
            name: "",
            capability: preset?.capability ?? "",
            model: null,
            connectionId: preset?.connectionId ?? "",
            temperature: null,
            maxTokens: null,
            systemPromptTemplate: null,
            tags: [],
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a profile by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecuteAndNotify(
            this.#host,
            ProfilesService.getProfileById({ path: { id: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiProfileTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new profile.
     */
    async create(model: UaiProfileDetailModel, _parentUnique: string | null) {
        const requestBody = UaiProfileTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecuteAndNotify(
            this.#host,
            ProfilesService.postProfile({ body: requestBody })
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
     * Updates an existing profile.
     */
    async update(model: UaiProfileDetailModel) {
        const requestBody = UaiProfileTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecuteAndNotify(
            this.#host,
            ProfilesService.putProfileById({
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
     * Deletes a profile by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecuteAndNotify(
            this.#host,
            ProfilesService.deleteProfileById({ path: { id: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
