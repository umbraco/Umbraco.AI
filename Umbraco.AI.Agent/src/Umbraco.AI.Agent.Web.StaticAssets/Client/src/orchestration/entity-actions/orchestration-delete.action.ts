import { UaiDeleteActionBase, type UaiDeleteActionArgs } from "@umbraco-ai/core";
import { UaiOrchestrationDetailRepository } from "../repository/detail/orchestration-detail.repository.js";

export class UaiOrchestrationDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiOrchestration_deleteConfirm",
            getRepository: (host) => new UaiOrchestrationDetailRepository(host),
        };
    }
}

export { UaiOrchestrationDeleteAction as api };
