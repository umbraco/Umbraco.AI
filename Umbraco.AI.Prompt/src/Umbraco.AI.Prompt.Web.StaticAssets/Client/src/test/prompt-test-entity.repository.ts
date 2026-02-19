import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
	UaiTestFeatureEntityRepositoryApi,
	UaiTestFeatureEntityData,
} from "@umbraco-ai/core";
import { PromptsService } from "../api/sdk.gen.js";

/**
 * Repository for accessing prompts as test feature entities.
 * Provides prompt data for test target selection.
 */
export class PromptTestFeatureEntityRepository
	extends UmbControllerBase
	implements UaiTestFeatureEntityRepositoryApi
{
	constructor(host: UmbControllerHost) {
		super(host);
	}

	async getEntities(): Promise<UaiTestFeatureEntityData[]> {
		try {
			const response = await PromptsService.getAllPrompts();

			if (!response.data?.items) {
				return [];
			}

			return response.data.items.map((prompt) => ({
				id: prompt.id,
				name: prompt.name,
				description: prompt.alias,
				icon: "icon-command", // Prompt icon
			}));
		} catch (error) {
			console.error("Failed to load prompts for test picker:", error);
			return [];
		}
	}

	async getEntity(idOrAlias: string): Promise<UaiTestFeatureEntityData | undefined> {
		try {
			const response = await PromptsService.getPromptByIdOrAlias({
				path: { promptIdOrAlias: idOrAlias },
			});

			if (!response.data) {
				return undefined;
			}

			const prompt = response.data;
			return {
				id: prompt.alias,
				name: prompt.name,
				description: prompt.description || undefined,
				icon: "icon-script-alt",
			};
		} catch {
			return undefined;
		}
	}
}

export { PromptTestFeatureEntityRepository as api };
