import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { AgentsService } from "../../api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UaiCopilotAgentItem } from "../types.js";

/**
 * Repository for loading active agents for the copilot.
 */
export class UaiCopilotAgentRepository {
  #host: UmbControllerHost;

  constructor(host: UmbControllerHost) {
    this.#host = host;
  }

  async fetchActiveAgents(): Promise<{ data?: UaiCopilotAgentItem[]; error?: unknown }> {
    const response = await tryExecute(
      this.#host,
      AgentsService.getAllAgents({
        query: { skip: 0, take: 100 },
      })
    );

    if (response.error || !response.data) {
      return { error: response.error };
    }

    const items: UaiCopilotAgentItem[] = response.data.items
      .filter((agent) => agent.isActive)
      .map((agent) => ({
        id: agent.id!,
        name: agent.name!,
        alias: agent.alias!,
      }));

    return { data: items };
  }
}
