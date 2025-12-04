import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { PromptsService } from "../../../api/index.js";
import type { PromptItemResponseModel } from "../../../api/types.gen.js";
import type { UaiPromptRegistrationModel } from "../../property-actions/types.js";

/**
 * Server data source for fetching prompts for property action registration.
 * Only returns active prompts with minimal data needed for registration.
 */
export class UaiPromptRegistrarServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all active prompts for registration as property actions.
     */
    async getActivePrompts(): Promise<{ data?: UaiPromptRegistrationModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            PromptsService.getAllPrompts({
                query: {
                    skip: 0,
                    take: 1000, // Fetch all prompts - registration happens once
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        // Filter to only active prompts and fetch full details for content
        const activePrompts = data.items.filter((item: PromptItemResponseModel) => item.isActive);

        // We need to fetch full details for each prompt to get the content
        const promptDetails = await Promise.all(
            activePrompts.map(async (item: PromptItemResponseModel) => {
                const { data: detail, error: detailError } = await tryExecute(
                    this.#host,
                    PromptsService.getPromptByIdOrAlias({
                        path: { promptIdOrAlias: item.id },
                    })
                );

                if (detailError || !detail) {
                    return null;
                }

                return {
                    unique: detail.id,
                    alias: detail.alias,
                    name: detail.name,
                    description: detail.description ?? null,
                    content: detail.content,
                } satisfies UaiPromptRegistrationModel;
            })
        );

        return {
            data: promptDetails.filter((p): p is UaiPromptRegistrationModel => p !== null),
        };
    }
}
