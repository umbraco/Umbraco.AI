import { UaiDeleteActionBase, type UaiDeleteActionArgs } from "@umbraco-ai/core";
import { UAiAgentDetailRepository } from "../repository/detail/agent-detail.repository.js";

export class UAiAgentDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uAiAgent_deleteConfirm",
            getRepository: (host) => new UAiAgentDetailRepository(host),
        };
    }
}

export { UAiAgentDeleteAction as api };
