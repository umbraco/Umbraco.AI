import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { UaiConversationRepository } from "../../conversation/repository/conversation.repository.js";
import { copilotWorkspaceConversationPath, copilotWorkspaceProjectCreatePath } from "../../paths.js";

/**
 * "Create" entity actions on the section-root entity type ({@link UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE}),
 * surfaced by the sidebar header's ⋯/+ menu — so New chat / New project are standard entity actions
 * rather than a hand-rolled menu (mirrors the CMS create-on-collection convention).
 */
export class UaiCopilotWorkspaceNewChatAction extends UmbEntityActionBase<never> {
    override async execute(): Promise<void> {
        const { data } = await new UaiConversationRepository(this).create({});
        if (data?.id) {
            window.history.pushState({}, "", copilotWorkspaceConversationPath(data.id));
        }
    }
}

export class UaiCopilotWorkspaceNewProjectAction extends UmbEntityActionBase<never> {
    override async execute(): Promise<void> {
        // Opens the project workspace in create mode; it creates on Save (dispatching CREATED).
        window.history.pushState({}, "", copilotWorkspaceProjectCreatePath());
    }
}
