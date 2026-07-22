import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_CONVERSATION_ENTITY_CONTEXT } from "./conversation-entity.context.js";

export const UAI_CONVERSATION_IS_PINNED_CONDITION = "Uai.CopilotWorkspace.Condition.ConversationIsPinned";
export const UAI_CONVERSATION_IS_NOT_PINNED_CONDITION = "Uai.CopilotWorkspace.Condition.ConversationIsNotPinned";
export const UAI_CONVERSATION_IS_ARCHIVED_CONDITION = "Uai.CopilotWorkspace.Condition.ConversationIsArchived";
export const UAI_CONVERSATION_IS_NOT_ARCHIVED_CONDITION = "Uai.CopilotWorkspace.Condition.ConversationIsNotArchived";

/**
 * State conditions for conversation entity actions. Each consumes the per-node conversation context
 * and permits its action based on the live pin/archive flags, so Pin↔Unpin and Archive↔Unarchive are
 * mutually exclusive on a given node. Mirrors the CMS `UmbEntityIsTrashedCondition` pattern.
 */
export class UaiConversationIsPinnedCondition
    extends UmbConditionBase<UmbConditionConfigBase>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UmbConditionConfigBase>) {
        super(host, args);
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            this.observe(context?.isPinned, (isPinned) => {
                this.permitted = isPinned === true;
            });
        });
    }
}

export class UaiConversationIsNotPinnedCondition
    extends UmbConditionBase<UmbConditionConfigBase>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UmbConditionConfigBase>) {
        super(host, args);
        this.permitted = true;
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            this.observe(context?.isPinned, (isPinned) => {
                this.permitted = isPinned !== true;
            });
        });
    }
}

export class UaiConversationIsArchivedCondition
    extends UmbConditionBase<UmbConditionConfigBase>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UmbConditionConfigBase>) {
        super(host, args);
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            this.observe(context?.isArchived, (isArchived) => {
                this.permitted = isArchived === true;
            });
        });
    }
}

export class UaiConversationIsNotArchivedCondition
    extends UmbConditionBase<UmbConditionConfigBase>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UmbConditionConfigBase>) {
        super(host, args);
        this.permitted = true;
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            this.observe(context?.isArchived, (isArchived) => {
                this.permitted = isArchived !== true;
            });
        });
    }
}
