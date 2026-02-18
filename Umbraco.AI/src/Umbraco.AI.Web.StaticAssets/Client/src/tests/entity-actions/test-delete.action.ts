import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UMB_CONFIRM_MODAL } from "@umbraco-cms/backoffice/modal";
import { AITestRepository } from "../repository/test.repository.js";

/**
 * Entity action for deleting a test.
 * Shows confirmation modal before deleting.
 */
export class UaiTestDeleteEntityAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const unique = this.args.unique;
		if (!unique) return;

		const confirmed = await modalManager
			.open(this, UMB_CONFIRM_MODAL, {
				data: {
					headline: "Delete Test",
					content: "Are you sure you want to delete this test? This action cannot be undone.",
					color: "danger",
					confirmLabel: "Delete",
				},
			})
			.onSubmit()
			.catch(() => false);

		if (!confirmed) return;

		const repository = new AITestRepository(this);
		await repository.delete(unique);

		// Refresh the collection (the entity will be removed automatically)
		// TODO: Trigger collection refresh or use notification service
	}
}

export { UaiTestDeleteEntityAction as api };
