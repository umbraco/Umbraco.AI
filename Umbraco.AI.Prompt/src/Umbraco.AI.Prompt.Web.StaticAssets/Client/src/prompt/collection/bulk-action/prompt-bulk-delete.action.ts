import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from "@umbraco-ai/core";
import { UaiPromptDetailRepository } from "../../repository/detail/prompt-detail.repository.js";

export class UaiPromptBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiPrompt_bulkDeleteConfirm",
            getRepository: (host) => new UaiPromptDetailRepository(host),
        };
    }
}

export { UaiPromptBulkDeleteAction as api };
