import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_TEST_RUN_DETAIL_MODAL } from "../modals/test-run-detail/test-run-detail-modal.token.js";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../workspace/test/test-workspace.context-token.js";
import type { UaiTestWorkspaceContext } from "../workspace/test/test-workspace.context.js";

export class UaiTestRunViewDetailEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		const runId = this.args.unique;
		if (!runId) return;

		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		// Resolve baselineRunId from workspace context when inside a test workspace
		let baselineRunId: string | undefined;
		try {
			const workspaceContext = await Promise.race([
				this.getContext(UAI_TEST_WORKSPACE_CONTEXT) as Promise<UaiTestWorkspaceContext>,
				new Promise<undefined>((resolve) => setTimeout(() => resolve(undefined), 100)),
			]);
			baselineRunId = workspaceContext?.getData()?.baselineRunId ?? undefined;
		} catch {
			// Not inside a test workspace
		}

		modalManager.open(this, UAI_TEST_RUN_DETAIL_MODAL, {
			data: { runId, baselineRunId },
		});
	}
}

export { UaiTestRunViewDetailEntityAction as api };
