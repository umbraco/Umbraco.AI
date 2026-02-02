import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiAgentRepository } from "@umbraco-ai/agent";
import type { UaiCopilotAgentItem } from "../types.js";

/**
 * Repository for loading active agents for the copilot.
 * Wraps the UaiAgentRepository from @umbraco-ai/agent and maps to copilot-specific item type.
 */
export class UaiCopilotAgentRepository {
  #agentRepository: UaiAgentRepository;

  constructor(host: UmbControllerHost) {
    this.#agentRepository = new UaiAgentRepository(host);
  }

  async fetchActiveAgents(): Promise<{ data?: UaiCopilotAgentItem[]; error?: unknown }> {
    const response = await this.#agentRepository.fetchActiveAgents({
      scopeId: "copilot",
    });

    if (response.error || !response.data) {
      return { error: response.error };
    }

    // Map to lightweight copilot item type
    const items: UaiCopilotAgentItem[] = response.data.items.map((agent) => ({
      id: agent.unique,
      name: agent.name,
      alias: agent.alias,
    }));

    return { data: items };
  }
}
