import { UaiDeleteActionBase, type UaiDeleteActionArgs } from "@umbraco-ai/core";
import { UaiAgentDetailRepository } from "../repository/detail/agent-detail.repository.js";

export class UaiAgentDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uAiAgentCopilot_deleteConfirm",
            getRepository: (host) => new UaiAgentDetailRepository(host),
        };
    }
}

export { UaiAgentDeleteAction as api };
