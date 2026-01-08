import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN } from "../workspace/context/paths.js";

/**
 * Entity action for creating a new context.
 * Navigates directly to the create workspace.
 */
export class UaiContextCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const path = UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }
}

export { UaiContextCreateEntityAction as api };
