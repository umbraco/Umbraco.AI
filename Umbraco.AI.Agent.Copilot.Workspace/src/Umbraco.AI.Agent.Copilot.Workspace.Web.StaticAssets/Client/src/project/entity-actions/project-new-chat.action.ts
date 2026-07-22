import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UaiConversationRepository } from "../../conversation/repository/conversation.repository.js";
import { copilotWorkspaceConversationPath } from "../../paths.js";

/**
 * "New chat in this project" entity action (shown in the project workspace ⋯ menu). Creates a
 * conversation attached to the project and opens it.
 */
export class UaiCopilotWorkspaceProjectNewChatAction extends UmbEntityActionBase<never> {
    constructor(host: import("@umbraco-cms/backoffice/controller-api").UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute(): Promise<void> {
        const projectId = this.args.unique;
        if (!projectId) return;
        const { data } = await new UaiConversationRepository(this).create({ projectId });
        if (data?.id) {
            window.history.pushState({}, "", copilotWorkspaceConversationPath(data.id));
        }
    }
}

export { UaiCopilotWorkspaceProjectNewChatAction as api };
