import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN } from "../workspace/agent/paths.js";
import { UAI_AGENT_CREATE_OPTIONS_MODAL } from "../modals/create-options/agent-create-options-modal.token.js";

/**
 * Entity action for creating a new agent.
 * Opens the agent type selection modal before navigating to the create workspace.
 */
export class UaiAgentCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const result = await modalManager
            .open(this, UAI_AGENT_CREATE_OPTIONS_MODAL, {
                data: { headline: "Select Agent Type" },
            })
            .onSubmit()
            .catch(() => undefined);

        if (!result?.agentType) return;

        const path = UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN.generateAbsolute({});

        // Pass the agent type as a query parameter for the workspace to read
        history.pushState(null, "", `${path}?agentType=${result.agentType}`);
    }
}

export { UaiAgentCreateEntityAction as api };
