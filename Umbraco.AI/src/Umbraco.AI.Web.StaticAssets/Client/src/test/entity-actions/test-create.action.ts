import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN } from "../workspace/paths.js";
import { UAI_TEST_CREATE_OPTIONS_MODAL } from "../modals/create-options/test-create-options-modal.token.js";

/**
 * Entity action for creating a new test.
 * Opens the test feature selection modal before navigating to the create workspace.
 */
export class UaiTestCreateEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const result = await modalManager
			.open(this, UAI_TEST_CREATE_OPTIONS_MODAL, {
				data: { headline: "Select Test Feature" },
			})
			.onSubmit()
			.catch(() => undefined);

		if (!result?.testFeatureId) return;

		const path = UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN.generateAbsolute({
			testFeatureId: result.testFeatureId,
		});

		history.pushState(null, "", path);
	}
}

export { UaiTestCreateEntityAction as api };
