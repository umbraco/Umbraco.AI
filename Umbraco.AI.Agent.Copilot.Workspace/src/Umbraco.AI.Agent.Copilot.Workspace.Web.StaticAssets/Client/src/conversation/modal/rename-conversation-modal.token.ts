import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export const UAI_RENAME_CONVERSATION_MODAL_ALIAS = "Uai.CopilotWorkspace.Modal.RenameConversation";

export interface UaiRenameConversationModalData {
    /** The current title, pre-filled into the input. */
    value: string;
}

export interface UaiRenameConversationModalValue {
    /** The new (trimmed, non-empty) title. */
    name: string;
}

export const UAI_RENAME_CONVERSATION_MODAL = new UmbModalToken<
    UaiRenameConversationModalData,
    UaiRenameConversationModalValue
>(UAI_RENAME_CONVERSATION_MODAL_ALIAS, { modal: { type: "dialog", size: "small" } });
