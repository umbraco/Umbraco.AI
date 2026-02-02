import { UaiDeleteActionBase, type UaiDeleteActionArgs } from "@umbraco-ai/core";
import { UaiPromptDetailRepository } from "../repository/detail/prompt-detail.repository.js";

export class UaiPromptDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: "#actions_delete",
            confirmMessage: "#uaiPrompt_deleteConfirm",
            getRepository: (host) => new UaiPromptDetailRepository(host),
        };
    }
}

export { UaiPromptDeleteAction as api };
