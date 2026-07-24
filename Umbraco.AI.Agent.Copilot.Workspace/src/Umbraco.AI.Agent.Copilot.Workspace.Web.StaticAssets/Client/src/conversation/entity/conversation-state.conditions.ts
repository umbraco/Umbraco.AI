import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CONVERSATION_ENTITY_CONTEXT } from "./conversation-entity.context.js";

export const UAI_CONVERSATION_STATE_CONDITION = "Uai.CopilotWorkspace.Condition.ConversationState";

/** Which conversation flag (and polarity) permits the action. */
export type UaiConversationStateMatch = "pinned" | "notPinned" | "archived" | "notArchived";

export interface UaiConversationStateConditionConfig extends UmbConditionConfigBase {
    match: UaiConversationStateMatch;
}

/**
 * State condition for conversation entity actions. Consumes the per-node conversation context and
 * permits its action based on the live pin/archive flag named by `match`, so Pin↔Unpin and
 * Archive↔Unarchive stay mutually exclusive on a given node. Config-driven (one class, one manifest)
 * mirroring the sidebar group-not-empty condition; the CMS trashed/restore pattern is the inspiration.
 * Fail-safe for the "not" polarities: permitted defaults to true until the context resolves.
 */
export class UaiConversationStateCondition
    extends UmbConditionBase<UaiConversationStateConditionConfig>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiConversationStateConditionConfig>) {
        super(host, args);
        const match = this.config.match;
        // "not*" actions should show by default (before the context resolves); "is*" should not.
        this.permitted = match === "notPinned" || match === "notArchived";
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            if (!context) return;
            const flag$ = match === "pinned" || match === "notPinned" ? context.isPinned : context.isArchived;
            const wantTrue = match === "pinned" || match === "archived";
            this.observe(flag$, (value) => {
                this.permitted = (value === true) === wantTrue;
            });
        });
    }
}

export { UaiConversationStateCondition as api };
