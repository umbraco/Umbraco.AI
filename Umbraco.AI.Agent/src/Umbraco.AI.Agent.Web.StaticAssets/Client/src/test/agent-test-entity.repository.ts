import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
	UaiTestFeatureEntityRepositoryApi,
	UaiTestFeatureEntityData,
} from "@umbraco-ai/core";
import { AgentsService } from "../api/sdk.gen.js";

/**
 * Repository for accessing agents as test feature entities.
 * Provides agent data for test target selection.
 */
export class AgentTestFeatureEntityRepository extends UmbControllerBase implements UaiTestFeatureEntityRepositoryApi {
	constructor(host: UmbControllerHost) {
		super(host);
	}

	async getEntities(): Promise<UaiTestFeatureEntityData[]> {
		try {
			const response = await AgentsService.getAllAgents();

			if (!response.data?.items) {
				return [];
			}

			return response.data.items.map((agent) => ({
				id: agent.alias, // Use alias as primary identifier
				name: agent.name,
				description: agent.description || undefined,
				icon: "icon-robot", // Agent icon
			}));
		} catch (error) {
			console.error("Failed to load agents for test picker:", error);
			return [];
		}
	}

	async getEntity(idOrAlias: string): Promise<UaiTestFeatureEntityData | undefined> {
		try {
			const response = await AgentsService.getAgentByIdOrAlias({
				path: { agentIdOrAlias: idOrAlias },
			});

			if (!response.data) {
				return undefined;
			}

			const agent = response.data;
			return {
				id: agent.alias,
				name: agent.name,
				description: agent.description || undefined,
				icon: "icon-robot",
			};
		} catch {
			return undefined;
		}
	}
}

export { AgentTestFeatureEntityRepository as api };
