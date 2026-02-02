import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
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
        const capability = preset?.capability ?? "";

        // Create capability-appropriate default settings
        const settings = this.#createDefaultSettings(capability);

        const scaffold: UaiProfileDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_PROFILE_ENTITY_TYPE,
            alias: "",
            name: "",
            capability,
            model: null,
            connectionId: "",
            settings,
            tags: [],
            dateCreated: null,
            dateModified: null,
            version: 0,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Creates default settings based on capability.
     */
    #createDefaultSettings(capability: string): UaiProfileDetailModel["settings"] {
        switch (capability.toLowerCase()) {
            case "chat":
                return {
                    $type: "chat",
                    temperature: null,
                    maxTokens: null,
                    systemPromptTemplate: null,
                    contextIds: [],
                } as UaiProfileDetailModel["settings"];
            case "embedding":
                return {
                    $type: "embedding",
                } as UaiProfileDetailModel["settings"];
            default:
                return null;
        }
    }

    /**
     * Reads a profile by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            ProfilesService.getProfileByIdOrAlias({ path: { profileIdOrAlias: unique } })
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

        const { response, error } = await tryExecute(
            this.#host,
            ProfilesService.createProfile({ body: requestBody })
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

        const { error } = await tryExecute(
            this.#host,
            ProfilesService.updateProfile({
                path: { profileIdOrAlias: model.unique },
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
     * Deletes a profile by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            ProfilesService.deleteProfile({ path: { profileIdOrAlias: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
