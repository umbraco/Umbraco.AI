import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { AgentsService } from "../../api/sdk.gen.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";

export interface CopilotAgentItem {
  id: string;
  name: string;
  alias: string;
}

/**
 * Repository for loading active agents for the copilot.
 */
export class UaiCopilotRepository extends UmbRepositoryBase {
  constructor(host: UmbControllerHost) {
    super(host);
  }

  async requestActiveAgents(): Promise<{ data?: CopilotAgentItem[]; error?: unknown }> {
    const { data, error } = await tryExecute(
      this,
      AgentsService.getAllAgents({
        query: { skip: 0, take: 100 },
      })
    );

    if (error || !data) {
      return { error };
    }

    // Filter to only active agents on the client side
    const items: CopilotAgentItem[] = data.items
      .filter((agent) => agent.isActive)
      .map((agent) => ({
        id: agent.id!,
        name: agent.name!,
        alias: agent.alias!,
      }));

    return { data: items };
  }
}
