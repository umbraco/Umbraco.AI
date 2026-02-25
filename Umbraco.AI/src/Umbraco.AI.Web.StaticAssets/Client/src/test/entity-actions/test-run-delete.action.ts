import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { AITestRepository } from "../repository/test.repository.js";

/**
 * Entity action for deleting a test run.
 * Shows confirmation modal before deleting.
 */
export class UaiTestRunDeleteEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		const unique = this.args.unique;
		if (!unique) return;

		await umbConfirmModal(this, {
			headline: "Delete Test Run",
			content: "Are you sure you want to delete this test run? This action cannot be undone.",
			color: "danger",
			confirmLabel: "Delete",
		});

		const repository = new AITestRepository(this);
		await repository.deleteRun(unique);

		const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
		collectionContext?.loadCollection();
	}
}

export { UaiTestRunDeleteEntityAction as api };
