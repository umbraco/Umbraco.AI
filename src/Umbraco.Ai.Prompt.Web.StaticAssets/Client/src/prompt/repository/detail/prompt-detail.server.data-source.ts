import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbDetailDataSource } from "@umbraco-cms/backoffice/repository";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
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
            fetch(`/umbraco/ai/management/api/v1/prompts/${unique}`, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' },
            }).then(res => res.ok ? res.json() : Promise.reject(res))
        );

        if (error || !data) {
            return { error };
        }

        return { data: this.#mapResponseToModel(data) };
    }

    /**
     * Creates a new prompt.
     */
    async create(model: UaiPromptDetailModel, _parentUnique: string | null) {
        const requestBody = this.#mapModelToCreateRequest(model);

        const response = await fetch('/umbraco/ai/management/api/v1/prompts', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody),
        });

        if (!response.ok) {
            return { error: new Error('Failed to create prompt') };
        }

        // Extract the ID from the Location header
        const locationHeader = response.headers?.get("Location") ?? "";
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
        const requestBody = this.#mapModelToUpdateRequest(model);

        const { error } = await tryExecute(
            this.#host,
            fetch(`/umbraco/ai/management/api/v1/prompts/${model.unique}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody),
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
            fetch(`/umbraco/ai/management/api/v1/prompts/${unique}`, {
                method: 'DELETE',
            })
        );

        if (error) {
            return { error };
        }

        return {};
    }

    #mapResponseToModel(response: Record<string, unknown>): UaiPromptDetailModel {
        return {
            unique: response.id as string,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias as string,
            name: response.name as string,
            description: (response.description as string) ?? null,
            content: response.content as string,
            profileId: (response.profileId as string) ?? null,
            tags: (response.tags as string[]) ?? [],
            isActive: response.isActive as boolean,
        };
    }

    #mapModelToCreateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            content: model.content,
            profileId: model.profileId,
            tags: model.tags,
            isActive: model.isActive,
        };
    }

    #mapModelToUpdateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            content: model.content,
            profileId: model.profileId,
            tags: model.tags,
            isActive: model.isActive,
        };
    }
}
