import type { UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN } from "../workspace/agent/paths.js";
import { UAI_AGENT_CREATE_OPTIONS_MODAL } from "../modals/create-options/agent-create-options-modal.token.js";
import { AgentsService } from "../../api/sdk.gen.js";

/**
 * Entity action for creating a new agent.
 * If workflows are available, opens the agent type selection modal.
 * Otherwise, navigates directly to create a standard agent.
 */
export class UaiAgentCreateEntityAction extends UmbEntityActionBase<never> {
    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
    }

    override async execute() {
        const { data } = await tryExecute(this, AgentsService.getAgentWorkflows());
        const hasWorkflows = Array.isArray(data) && data.length > 0;

        let agentType: string;

        if (hasWorkflows) {
            const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
            if (!modalManager) return;

            const result = await modalManager
                .open(this, UAI_AGENT_CREATE_OPTIONS_MODAL, {
                    data: { headline: "Select Agent Type" },
                })
                .onSubmit()
                .catch(() => undefined);

            if (!result?.agentType) return;
            agentType = result.agentType;
        } else {
            agentType = "standard";
        }

        const path = UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ agentType });
        history.pushState(null, "", path);
    }
}

export { UaiAgentCreateEntityAction as api };
