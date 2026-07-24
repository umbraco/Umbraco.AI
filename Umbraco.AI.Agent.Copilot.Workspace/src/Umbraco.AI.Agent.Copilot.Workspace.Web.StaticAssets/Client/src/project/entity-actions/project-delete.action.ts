import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UaiProjectRepository } from "../repository/project.repository.js";

/**
 * Delete entity action for a project (shown in the workspace ⋯ menu). Confirms, then deletes via the
 * reactive repository — which dispatches DELETED so the sidebar tree updates and the workspace's
 * deleted-redirect returns the user to the section root. Modal strings are localization keys
 * (umbConfirmModal resolves them).
 */
export class UaiCopilotWorkspaceProjectDeleteAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute(): Promise<void> {
        const unique = this.args.unique;
        if (!unique) return;
        await umbConfirmModal(this, {
            headline: "#uaiCopilotWorkspace_projectDeleteConfirmTitle",
            content: "#uaiCopilotWorkspace_projectDeleteConfirmMessage",
            color: "danger",
            confirmLabel: "#actions_delete",
        });
        await new UaiProjectRepository(this).delete(unique);
    }
}

export { UaiCopilotWorkspaceProjectDeleteAction as api };
