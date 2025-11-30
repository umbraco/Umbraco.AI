import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN } from "../workspace/prompt/paths.js";

/**
 * Entity action for creating a new prompt.
 * Navigates directly to the create workspace.
 */
export class UaiPromptCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const path = UAI_CREATE_PROMPT_WORKSPACE_PATH_PATTERN.generateAbsolute({});
        history.pushState(null, "", path);
    }
}

export { UaiPromptCreateEntityAction as api };
