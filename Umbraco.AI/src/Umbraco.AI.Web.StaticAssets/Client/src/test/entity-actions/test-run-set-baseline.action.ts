import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UaiTestRunDetailRepository } from "../repository/test-run-detail/test-run-detail.repository.js";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../workspace/test/test-workspace.context-token.js";
import type { UaiTestWorkspaceContext } from "../workspace/test/test-workspace.context.js";
import { UaiPartialUpdateCommand } from "../../core/command/implement/partial-update.command.js";
import type { UaiTestDetailModel } from "../types.js";

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

		const repository = new UaiTestRunDetailRepository(this);

		// Try workspace context first (inside a test workspace), fall back to fetching the run
		let testId: string | undefined;
		let workspaceContext: UaiTestWorkspaceContext | undefined;

		try {
			workspaceContext = await Promise.race([
				this.getContext(UAI_TEST_WORKSPACE_CONTEXT) as Promise<UaiTestWorkspaceContext>,
				new Promise<undefined>((resolve) => setTimeout(() => resolve(undefined), 100)),
			]) ?? undefined;
			testId = workspaceContext?.getUnique() ?? undefined;
		} catch {
			// Not inside a test workspace
		}

		if (!testId) {
			const { data: run } = await repository.requestById(runId);
			testId = run?.testId;
		}

		if (!testId) return;

		const { error } = await repository.requestSetBaseline(testId, runId);

		if (error) {
			const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
			notificationContext?.peek("danger", {
				data: { headline: "Error", message: "Failed to set baseline." },
			});
			return;
		}

		const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
		notificationContext?.peek("positive", {
			data: { headline: "Baseline Set", message: "Test run has been set as the baseline." },
		});

		// Update baselineRunId on the model in-place when inside a test workspace
		if (workspaceContext) {
			workspaceContext.handleCommand(
				new UaiPartialUpdateCommand<UaiTestDetailModel>({ baselineRunId: runId }, "baseline"),
			);
		}

		// Refresh the collection to update baseline indicators
		const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
		collectionContext?.loadCollection();
	}
}

export { UaiTestRunSetBaselineEntityAction as api };
