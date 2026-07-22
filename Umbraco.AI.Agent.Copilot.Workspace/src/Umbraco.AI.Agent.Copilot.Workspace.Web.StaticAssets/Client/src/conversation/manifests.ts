import { UAI_CONVERSATION_ENTITY_TYPE } from "../constants.js";
import {
    UaiConversationPinAction,
    UaiConversationUnpinAction,
    UaiConversationArchiveAction,
    UaiConversationUnarchiveAction,
    UaiConversationRenameAction,
    UaiConversationMoveAction,
    UaiConversationDeleteAction,
} from "./entity-actions/conversation.actions.js";
import {
    UaiConversationIsPinnedCondition,
    UaiConversationIsNotPinnedCondition,
    UaiConversationIsArchivedCondition,
    UaiConversationIsNotArchivedCondition,
    UAI_CONVERSATION_IS_PINNED_CONDITION,
    UAI_CONVERSATION_IS_NOT_PINNED_CONDITION,
    UAI_CONVERSATION_IS_ARCHIVED_CONDITION,
    UAI_CONVERSATION_IS_NOT_ARCHIVED_CONDITION,
} from "./entity/conversation-state.conditions.js";
import UaiRenameConversationModalElement from "./modal/rename-conversation-modal.element.js";
import { UAI_RENAME_CONVERSATION_MODAL_ALIAS } from "./modal/rename-conversation-modal.token.js";

const forConversation = { forEntityTypes: [UAI_CONVERSATION_ENTITY_TYPE] };

/**
 * Conversation entity extensions: the ⋯-menu entity actions rendered by each sidebar tree item
 * (pin/unpin, rename, move, archive/unarchive, delete), their pin/archive state conditions, and the
 * rename modal. The per-node conversation context (provided by the tree item) drives the conditions.
 */
export const conversationManifests: UmbExtensionManifest[] = [
    {
        type: "modal",
        alias: UAI_RENAME_CONVERSATION_MODAL_ALIAS,
        name: "Rename Conversation Modal",
        element: UaiRenameConversationModalElement,
    },

    {
        type: "condition",
        alias: UAI_CONVERSATION_IS_PINNED_CONDITION,
        name: "Conversation Is Pinned Condition",
        api: UaiConversationIsPinnedCondition,
    },
    {
        type: "condition",
        alias: UAI_CONVERSATION_IS_NOT_PINNED_CONDITION,
        name: "Conversation Is Not Pinned Condition",
        api: UaiConversationIsNotPinnedCondition,
    },
    {
        type: "condition",
        alias: UAI_CONVERSATION_IS_ARCHIVED_CONDITION,
        name: "Conversation Is Archived Condition",
        api: UaiConversationIsArchivedCondition,
    },
    {
        type: "condition",
        alias: UAI_CONVERSATION_IS_NOT_ARCHIVED_CONDITION,
        name: "Conversation Is Not Archived Condition",
        api: UaiConversationIsNotArchivedCondition,
    },

    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Pin",
        name: "Pin Conversation Entity Action",
        weight: 600,
        api: UaiConversationPinAction,
        ...forConversation,
        meta: { icon: "icon-pushpin", label: "#uaiCopilotWorkspace_actionPin" },
        conditions: [{ alias: UAI_CONVERSATION_IS_NOT_PINNED_CONDITION }],
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Unpin",
        name: "Unpin Conversation Entity Action",
        weight: 600,
        api: UaiConversationUnpinAction,
        ...forConversation,
        meta: { icon: "icon-pushpin", label: "#uaiCopilotWorkspace_actionUnpin" },
        conditions: [{ alias: UAI_CONVERSATION_IS_PINNED_CONDITION }],
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Rename",
        name: "Rename Conversation Entity Action",
        weight: 500,
        api: UaiConversationRenameAction,
        ...forConversation,
        meta: { icon: "icon-edit", label: "#uaiCopilotWorkspace_actionRename" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Move",
        name: "Move Conversation Entity Action",
        weight: 400,
        api: UaiConversationMoveAction,
        ...forConversation,
        meta: { icon: "icon-enter", label: "#uaiCopilotWorkspace_actionMoveToProject" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Archive",
        name: "Archive Conversation Entity Action",
        weight: 300,
        api: UaiConversationArchiveAction,
        ...forConversation,
        meta: { icon: "icon-box", label: "#uaiCopilotWorkspace_actionArchive" },
        conditions: [{ alias: UAI_CONVERSATION_IS_NOT_ARCHIVED_CONDITION }],
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Unarchive",
        name: "Unarchive Conversation Entity Action",
        weight: 300,
        api: UaiConversationUnarchiveAction,
        ...forConversation,
        meta: { icon: "icon-box", label: "#uaiCopilotWorkspace_actionUnarchive" },
        conditions: [{ alias: UAI_CONVERSATION_IS_ARCHIVED_CONDITION }],
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Conversation.Delete",
        name: "Delete Conversation Entity Action",
        weight: 100,
        api: UaiConversationDeleteAction,
        ...forConversation,
        meta: { icon: "icon-trash", label: "#uaiCopilotWorkspace_actionDelete" },
    },
];
