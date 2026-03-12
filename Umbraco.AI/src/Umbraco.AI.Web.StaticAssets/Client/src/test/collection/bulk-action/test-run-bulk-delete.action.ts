import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UaiTestRunDetailRepository } from "../../repository/test-run-detail/test-run-detail.repository.js";

/**
 * Bulk action for deleting test runs.
 */
export class UaiTestRunBulkDeleteAction extends UmbEntityBulkActionBase<never> {
    async execute() {
        const count = this.selection.length;

        await umbConfirmModal(this, {
            headline: "#actions_delete",
            content: `Are you sure you want to delete ${count} test run(s)? This action cannot be undone.`,
            color: "danger",
            confirmLabel: "#actions_delete",
        });

        const repository = new UaiTestRunDetailRepository(this);
        for (const runId of this.selection) {
            await repository.requestDelete(runId);
        }

        const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
        collectionContext?.loadCollection();
    }
}

export { UaiTestRunBulkDeleteAction as api };
