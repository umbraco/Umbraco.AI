import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "./profile-workspace.context-token.js";

export const UAI_PROFILE_CAPABILITY_CONDITION_ALIAS = "UmbracoAI.Condition.ProfileCapability";

/**
 * Condition config that matches against the current profile's capability.
 */
export interface UaiProfileCapabilityConditionConfig extends UmbConditionConfigBase {
    /**
     * The capability to match (e.g., "chat" or "embedding").
     */
    match: string;
}

/**
 * Workspace condition that permits an extension only when the current
 * profile's capability matches the configured value.
 */
export class UaiProfileCapabilityCondition
    extends UmbConditionBase<UaiProfileCapabilityConditionConfig>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiProfileCapabilityConditionConfig>) {
        super(host, args);

        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                this.permitted = false;
                return;
            }

            this.observe(
                context.model,
                (model) => {
                    this.permitted = model?.capability?.toLowerCase() === this.config.match?.toLowerCase();
                },
                "profileCapabilityObserver",
            );
        });
    }
}

export { UaiProfileCapabilityCondition as api };

declare global {
    interface UmbExtensionConditionConfigMap {
        UAI_PROFILE_CAPABILITY_CONDITION: UaiProfileCapabilityConditionConfig;
    }
}
