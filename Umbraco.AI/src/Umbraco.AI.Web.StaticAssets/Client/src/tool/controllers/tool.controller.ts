import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { createExtensionApiByAlias } from "@umbraco-cms/backoffice/extension-registry";
import { UaiToolRepository } from "../repository/tool.repository.js";
import type { UaiFrontendToolRepositoryApi } from "@umbraco-ai/core";

/**
 * Controller for tool operations including scope and counting.
 * Proxies UaiToolRepository and adds tool counting functionality.
 */
export class UaiToolController extends UmbControllerBase {
	#toolRepository: UaiToolRepository;

	constructor(host: UmbControllerHost) {
		super(host);
		this.#toolRepository = new UaiToolRepository(host);
	}

	/**
	 * Gets all registered tool scopes.
	 * Proxies to UaiToolRepository.
	 */
	async getToolScopes() {
		return this.#toolRepository.getToolScopes();
	}

	/**
	 * Gets all registered tools.
	 * Proxies to UaiToolRepository.
	 */
	async getTools() {
		return this.#toolRepository.getTools();
	}

	/**
	 * Gets tool counts grouped by scope ID.
	 * Combines backend and frontend tools.
	 */
	async getToolCountsByScope(): Promise<Record<string, number>> {
		try {
			const counts: Record<string, number> = {};

			// Fetch backend tools
			const backendResponse = await this.#toolRepository.getTools();
			if (!backendResponse.error && backendResponse.data) {
				for (const tool of backendResponse.data) {
					counts[tool.scopeId] = (counts[tool.scopeId] ?? 0) + 1;
				}
			}

			// Fetch frontend tools
			try {
				// Get the frontend tool repository manifest by alias
                const frontendRepo = await createExtensionApiByAlias<UaiFrontendToolRepositoryApi>(this, "Uai.Repository.FrontendTool");
                if (frontendRepo) {
                    const frontendTools = await frontendRepo.getTools();
                    for (const tool of frontendTools) {
                        counts[tool.scopeId] = (counts[tool.scopeId] ?? 0) + 1;
                    }
                }
			} catch {
				// Frontend tool repository may not be available (Agent.UI not installed)
				// This is okay - just count backend tools
			}

			return counts;
		} catch {
			// Return empty map on error
			return {};
		}
	}
}
