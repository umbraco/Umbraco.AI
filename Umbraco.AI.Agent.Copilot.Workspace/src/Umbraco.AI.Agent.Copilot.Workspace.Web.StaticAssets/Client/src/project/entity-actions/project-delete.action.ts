import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UaiProjectRepository } from "../repository/project.repository.js";
import type { ProblemDetails } from "../../api/types.gen.js";

/**
 * Delete entity action for a project (shown in the workspace ⋯ menu). Confirms, then deletes via the
 * reactive repository — which dispatches DELETED so the sidebar tree updates and the workspace's
 * deleted-redirect returns the user to the section root. Modal strings are localization keys
 * (umbConfirmModal resolves them). This product's generated API client never sets `throwOnError`, so a
 * failed delete resolves with `{ error }` instead of throwing — `tryExecute`'s automatic error
 * notification never fires, and the failure must be surfaced here explicitly.
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
        const { error } = await new UaiProjectRepository(this).delete(unique);
        if (error) {
            const problemDetails = error as ProblemDetails;
            const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
            notificationContext?.peek("danger", {
                data: {
                    headline: problemDetails.title ?? "Delete failed",
                    message: problemDetails.detail ?? "The project could not be deleted.",
                },
            });
        }
    }
}

export { UaiCopilotWorkspaceProjectDeleteAction as api };
