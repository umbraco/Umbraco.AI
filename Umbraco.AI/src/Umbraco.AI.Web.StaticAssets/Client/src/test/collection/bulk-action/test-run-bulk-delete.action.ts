import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { UMB_MODAL_MANAGER_CONTEXT, UMB_CONFIRM_MODAL } from "@umbraco-cms/backoffice/modal";
import { AITestRepository } from "../../repository/test.repository.js";

/**
 * Bulk action for deleting test runs.
 */
export class UaiTestRunBulkDeleteAction extends UmbEntityBulkActionBase<never> {
    async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const count = this.selection.length;
        const confirmed = await modalManager
            .open(this, UMB_CONFIRM_MODAL, {
                data: {
                    headline: "#actions_delete",
                    content: `Are you sure you want to delete ${count} test run(s)? This action cannot be undone.`,
                    color: "danger",
                    confirmLabel: "#actions_delete",
                },
            })
            .onSubmit()
            .catch(() => false);

        if (!confirmed) return;

        const repository = new AITestRepository(this);
        for (const runId of this.selection) {
            await repository.deleteRun(runId);
        }
    }
}

export { UaiTestRunBulkDeleteAction as api };
