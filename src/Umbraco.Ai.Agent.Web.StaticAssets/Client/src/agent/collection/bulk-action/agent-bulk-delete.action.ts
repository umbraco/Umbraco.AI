import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from "@umbraco-ai/core";
import { UAiAgentDetailRepository } from "../../repository/detail/agent-detail.repository.js";

export class UAiAgentBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uAiAgent_bulkDeleteConfirm",
            getRepository: (host) => new UAiAgentDetailRepository(host),
        };
    }
}

export { UAiAgentBulkDeleteAction as api };
