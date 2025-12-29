import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from "@umbraco-ai/core";
import { UaiAgentDetailRepository } from "../../repository/detail/agent-detail.repository.js";

export class UaiAgentBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uAiAgent_bulkDeleteConfirm",
            getRepository: (host) => new UaiAgentDetailRepository(host),
        };
    }
}

export { UaiAgentBulkDeleteAction as api };
