import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { copilotWorkspaceConversationCreatePath, navigateToWorkspacePath } from "../../paths.js";

/**
 * "New chat in this project" entity action (shown in the project workspace ⋯ menu). Opens a draft
 * conversation pre-attached to the project; it's only persisted once the first message is sent.
 */
export class UaiCopilotWorkspaceProjectNewChatAction extends UmbEntityActionBase<never> {
    constructor(
        host: import("@umbraco-cms/backoffice/controller-api").UmbControllerHost,
        args: UmbEntityActionArgs<never>,
    ) {
        super(host, args);
    }

    override async execute(): Promise<void> {
        const projectId = this.args.unique;
        if (!projectId) return;
        navigateToWorkspacePath(copilotWorkspaceConversationCreatePath(projectId));
    }
}

export { UaiCopilotWorkspaceProjectNewChatAction as api };
