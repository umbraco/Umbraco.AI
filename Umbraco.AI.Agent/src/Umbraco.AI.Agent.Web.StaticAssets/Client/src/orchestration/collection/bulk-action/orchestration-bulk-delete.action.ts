import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from "@umbraco-ai/core";
import { UaiOrchestrationDetailRepository } from "../../repository/detail/orchestration-detail.repository.js";

export class UaiOrchestrationBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiOrchestration_bulkDeleteConfirm",
            getRepository: (host) => new UaiOrchestrationDetailRepository(host),
        };
    }
}

export { UaiOrchestrationBulkDeleteAction as api };
