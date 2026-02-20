import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { UMB_MODAL_MANAGER_CONTEXT, UMB_CONFIRM_MODAL } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { AITestRepository } from "../../repository/test.repository.js";

/**
 * Bulk action for running selected tests.
 */
export class UaiTestBulkRunAction extends UmbEntityBulkActionBase<never> {
    async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const count = this.selection.length;

        const confirmed = await modalManager
            .open(this, UMB_CONFIRM_MODAL, {
                data: {
                    headline: "Run Tests",
                    content: `Run ${count} selected test(s)? This will execute against configured AI providers.`,
                    color: "positive",
                    confirmLabel: "Run",
                },
            })
            .onSubmit()
            .catch(() => false);

        if (!confirmed) return;

        const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);

        try {
            const repository = new AITestRepository(this);
            const result = await repository.runByIds(this.selection as string[]);

            const passed = Object.values(result.results).filter(
                (m) => m.passAtK > 0,
            ).length;
            const failed = Object.values(result.results).length - passed;

            const status = failed === 0 ? "positive" : "warning";
            const headline = failed === 0 ? "All Tests Passed" : "Tests Completed";
            const message = `${passed} passed, ${failed} failed out of ${count} test(s)`;

            notificationContext?.peek(status, {
                data: { headline, message },
            });
        } catch (error) {
            notificationContext?.peek("danger", {
                data: {
                    headline: "Batch Run Failed",
                    message: error instanceof Error ? error.message : "An unexpected error occurred.",
                },
            });
        }
    }
}

export { UaiTestBulkRunAction as api };
