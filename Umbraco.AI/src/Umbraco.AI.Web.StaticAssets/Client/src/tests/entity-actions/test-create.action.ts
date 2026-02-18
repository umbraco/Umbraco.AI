import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN } from "../workspace/paths.js";

/**
 * Entity action for creating a new test.
 * Navigates to the create workspace with the test type (defaults to "prompt").
 */
export class UaiTestCreateEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		// Default to prompt test type
		// TODO: Show modal to select test type (prompt vs agent)
		const testType = "prompt";

		const path = UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN.generateAbsolute({
			testType,
		});

		history.pushState(null, "", path);
	}
}

export { UaiTestCreateEntityAction as api };
