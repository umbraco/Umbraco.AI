import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";
import { UaiAgentTypeMapper } from "../../type-mapper.js";

/**
 * Options for fetching agents.
 * @public
 */
export interface UaiAgentRepositoryOptions {
	/**
	 * Filter agents by scope ID (e.g., "copilot").
	 */
	scopeId?: string;

	/**
	 * Maximum number of agents to return.
	 */
	take?: number;
}

/**
 * Read-only repository for fetching active agents.
 * Provides a focused API for consumers that only need to read active agent data.
 * @public
 */
export class UaiAgentRepository {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	/**
	 * Fetches active agents with optional filtering.
	 * Only returns agents where isActive is true.
	 * @param options - Optional filtering and pagination options
	 * @returns Active agents matching the criteria
	 */
	async fetchActiveAgents(options?: UaiAgentRepositoryOptions) {
		const { data, error } = await tryExecute(
			this.#host,
			AgentsService.getAllAgents({
				query: {
					skip: 0,
					take: options?.take ?? 100,
					scopeId: options?.scopeId,
					isActive: true, // Always filter to active agents only
				},
			})
		);

		if (error || !data) {
			return { error };
		}

		// Map to item model (filtering now happens server-side)
		const items = data.items.map(UaiAgentTypeMapper.toItemModel);

		return {
			data: {
				items,
				total: data.total,
			},
		};
	}
}
