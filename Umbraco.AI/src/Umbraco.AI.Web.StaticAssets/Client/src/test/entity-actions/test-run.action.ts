import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UaiTestExecutionRepository } from "../repository/test-execution/test-execution.repository.js";
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

        const repository = new UaiTestExecutionRepository(this);
        const { data: metrics, error } = await repository.requestRunTest(unique);

        if (error || !metrics) {
            notificationContext?.peek("danger", {
                data: {
                    headline: "Test Run Failed",
                    message: "An unexpected error occurred.",
                },
            });
            return;
        }

        const status = metrics.passAtK > 0 ? "positive" : "warning";
        const headline = metrics.passAtK > 0 ? "Test Passed" : "Test Failed";
        const message = `Pass@K: ${(metrics.passAtK * 100).toFixed(0)}% | ${metrics.totalRuns} run(s)`;

        notificationContext?.peek(status, {
            data: { headline, message },
        });

        this.#signalTestRunCompleted();
    }

    #signalTestRunCompleted() {
        (this.getContext(UAI_TEST_WORKSPACE_CONTEXT) as Promise<typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE | undefined>).then(
            (context) => context?.signalTestRunCompleted(),
        );
    }
}

export { UaiTestRunEntityAction as api };
