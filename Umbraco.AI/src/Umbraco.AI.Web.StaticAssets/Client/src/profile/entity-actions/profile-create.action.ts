import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN } from "../workspace/profile/paths.js";
import { UAI_PROFILE_CREATE_OPTIONS_MODAL } from "../modals/create-options/profile-create-options-modal.token.js";

/**
 * Entity action for creating a new profile.
 * Opens the capability selection modal before navigating to the create workspace.
 */
export class UaiProfileCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const result = await modalManager
            .open(this, UAI_PROFILE_CREATE_OPTIONS_MODAL, {
                data: { headline: "Create Profile" },
            })
            .onSubmit()
            .catch(() => undefined);

        if (!result?.capability) return;

        const path = UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN.generateAbsolute({
            capability: result.capability,
        });

        history.pushState(null, "", path);
    }
}

export { UaiProfileCreateEntityAction as api };
