import type { UmbEntityFlag } from "@umbraco-cms/backoffice/entity-flag";
import type { ConversationResponseModel } from "./types.js";
import { UAI_CONVERSATION_SHARED_FLAG } from "../constants.js";

/**
 * The `entityFlags` input for the "shared" entity-sign badge rendered by
 * `uai-copilot-workspace-conversation-tree-item`: the shared flag when the conversation has at least one
 * active share, otherwise none. Pulled out as a pure function (rather than inlined in `render()`) so it's
 * independently testable without mounting the element — see `tree-item.helpers.test.ts`.
 */
export function conversationSignFlags(conversation: ConversationResponseModel): UmbEntityFlag[] {
    return conversation.hasActiveShares ? [{ alias: UAI_CONVERSATION_SHARED_FLAG }] : [];
}

/** The tree item's main row icon: a pushpin for a pinned conversation, otherwise the chat bubble. */
export function conversationIconName(conversation: ConversationResponseModel): string {
    return conversation.isPinned ? "icon-pushpin" : "icon-chat";
}
