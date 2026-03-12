import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "./agent-workspace.context-token.js";
import type { UaiAgentType } from "../../types.js";

export const UAI_AGENT_TYPE_CONDITION_ALIAS = "UmbracoAIAgent.Condition.AgentType";

/**
 * Condition config that matches against the current agent's agentType.
 */
export interface UaiAgentTypeConditionConfig extends UmbConditionConfigBase {
    /**
     * The agent type to match (e.g., "standard" or "orchestrated").
     */
    match: UaiAgentType;
}

/**
 * Workspace condition that permits an extension only when the current
 * agent's type matches the configured value.
 */
export class UaiAgentTypeCondition
    extends UmbConditionBase<UaiAgentTypeConditionConfig>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiAgentTypeConditionConfig>) {
        super(host, args);

        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                this.permitted = false;
                return;
            }

            this.observe(
                context.model,
                (model) => {
                    this.permitted = model?.agentType === this.config.match;
                },
                "agentTypeObserver",
            );
        });
    }
}

export { UaiAgentTypeCondition as api };
