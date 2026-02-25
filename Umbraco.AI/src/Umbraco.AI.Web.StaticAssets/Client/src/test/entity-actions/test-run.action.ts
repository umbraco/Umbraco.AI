import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { AITestRepository } from "../repository/test.repository.js";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../workspace/test/test-workspace.context-token.js";

/**
 * Entity action for running a test.
 * Executes the test and shows a toast notification with pass/fail summary.
 */
export class UaiTestRunEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const unique = this.args.unique;
        if (!unique) return;

        const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);

        try {
            const repository = new AITestRepository(this);
            const metrics = await repository.runTest(unique);

            const status = metrics.passAtK > 0 ? "positive" : "warning";
            const headline = metrics.passAtK > 0 ? "Test Passed" : "Test Failed";
            const message = `Pass@K: ${(metrics.passAtK * 100).toFixed(0)}% | ${metrics.totalRuns} run(s)`;

            notificationContext?.peek(status, {
                data: { headline, message },
            });

            this.#signalTestRunCompleted();
        } catch (error) {
            notificationContext?.peek("danger", {
                data: {
                    headline: "Test Run Failed",
                    message: error instanceof Error ? error.message : "An unexpected error occurred.",
                },
            });
        }
    }

    #signalTestRunCompleted() {
        (this.getContext(UAI_TEST_WORKSPACE_CONTEXT) as Promise<typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE | undefined>).then(
            (context) => context?.signalTestRunCompleted(),
        );
    }
}

export { UaiTestRunEntityAction as api };
