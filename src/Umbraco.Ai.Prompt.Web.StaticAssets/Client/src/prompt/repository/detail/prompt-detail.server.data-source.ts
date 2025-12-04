import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { PromptsService } from "../../../api/index.js";
import { UaiPromptTypeMapper } from "../../type-mapper.js";
import type { UaiPromptDetailModel } from "../../types.js";
import { UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";

const UAI_EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/**
 * Server data source for Prompt detail operations.
 */
export class UaiPromptDetailServerDataSource implements UmbDetailDataSource<UaiPromptDetailModel> {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Creates a scaffold for a new prompt.
     */
    async createScaffold(preset?: Partial<UaiPromptDetailModel>) {
        const scaffold: UaiPromptDetailModel = {
            unique: UAI_EMPTY_GUID,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: "",
            name: "",
            description: null,
            content: "",
            profileId: null,
            tags: [],
            scope: null,
            isActive: true,
            ...preset,
        };

        return { data: scaffold };
    }

    /**
     * Reads a prompt by its unique identifier.
     */
    async read(unique: string) {
        const { data, error } = await tryExecute(
            this.#host,
            PromptsService.getPromptByIdOrAlias({ path: { promptIdOrAlias: unique } })
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiPromptTypeMapper.toDetailModel(data) };
    }

    /**
     * Creates a new prompt.
     */
    async create(model: UaiPromptDetailModel, _parentUnique: string | null) {
        const requestBody = UaiPromptTypeMapper.toCreateRequest(model);

        const { response, error } = await tryExecute(
            this.#host,
            PromptsService.createPrompt({ body: requestBody })
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
     * Updates an existing prompt.
     */
    async update(model: UaiPromptDetailModel) {
        const requestBody = UaiPromptTypeMapper.toUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            PromptsService.updatePrompt({
                path: { promptIdOrAlias: model.unique },
                body: requestBody,
            })
        );

        if (error) {
            return { error };
        }

        return { data: model };
    }

    /**
     * Deletes a prompt by its unique identifier.
     */
    async delete(unique: string) {
        const { error } = await tryExecute(
            this.#host,
            PromptsService.deletePrompt({ path: { promptIdOrAlias: unique } })
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
