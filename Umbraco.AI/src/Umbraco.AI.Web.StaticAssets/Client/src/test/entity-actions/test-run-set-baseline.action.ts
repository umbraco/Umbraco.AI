import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { AITestRepository } from "../repository/test.repository.js";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../workspace/test/test-workspace.context-token.js";

/**
 * Entity action for setting a test run as the baseline.
 */
export class UaiTestRunSetBaselineEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		const runId = this.args.unique;
		if (!runId) return;

		const workspaceContext = await this.getContext(UAI_TEST_WORKSPACE_CONTEXT);
		if (!workspaceContext) return;
		const testId = workspaceContext.getUnique();
		if (!testId) return;

		const repository = new AITestRepository(this);

		try {
			await repository.setBaseline(testId, runId);

			const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
			notificationContext?.peek("positive", {
				data: { headline: "Baseline Set", message: "Test run has been set as the baseline." },
			});

			// Refresh workspace model to pick up new baselineRunId
			await workspaceContext.reload();

			// Refresh collection table
			const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
			collectionContext?.loadCollection();
		} catch (error) {
			const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
			notificationContext?.peek("danger", {
				data: { headline: "Error", message: "Failed to set baseline." },
			});
		}
	}
}

export { UaiTestRunSetBaselineEntityAction as api };
