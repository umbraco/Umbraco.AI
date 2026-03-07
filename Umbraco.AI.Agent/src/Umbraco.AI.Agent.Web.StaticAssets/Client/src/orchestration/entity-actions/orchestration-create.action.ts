import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_ORCHESTRATION_WORKSPACE_PATH_PATTERN } from "../workspace/orchestration/paths.js";
import { UAI_ORCHESTRATION_PATTERN_TEMPLATE_MODAL } from "../modals/pattern-template/pattern-template-modal.token.js";

/**
 * Entity action for creating a new orchestration.
 * Opens pattern template modal first, then navigates to the create workspace.
 */
export class UaiOrchestrationCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_ORCHESTRATION_PATTERN_TEMPLATE_MODAL, {
            data: {},
        });

        const result = await modal.onSubmit().catch(() => undefined);
        if (!result) return;

        const path = UAI_CREATE_ORCHESTRATION_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", `${path}?template=${result.templateName}`);
    }
}

export { UaiOrchestrationCreateEntityAction as api };
