import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN } from "../workspace/connection/paths.js";
import { UAI_CONNECTION_CREATE_OPTIONS_MODAL } from "../modals/create-options/connection-create-options-modal.token.js";

/**
 * Entity action for creating a new connection.
 * Opens the provider selection modal before navigating to the create workspace.
 */
export class UaiConnectionCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const result = await modalManager
            .open(this, UAI_CONNECTION_CREATE_OPTIONS_MODAL, {
                data: { headline: "Select AI Provider" },
            })
            .onSubmit()
            .catch(() => undefined);

        if (!result?.providerAlias) return;

        const path = UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({
            providerAlias: result.providerAlias,
        });

        history.pushState(null, "", path);
    }
}

export { UaiConnectionCreateEntityAction as api };
