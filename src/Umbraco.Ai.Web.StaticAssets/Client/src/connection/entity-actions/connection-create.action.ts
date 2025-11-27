import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH } from "../workspace/connection/paths.js";

/**
 * Entity action for creating a new connection.
 * Used by the menu item to show a "+" button.
 */
export class UaiConnectionCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        history.pushState(null, "", UAI_CREATE_CONNECTION_WORKSPACE_PATH);
    }
}

export { UaiConnectionCreateEntityAction as api };
