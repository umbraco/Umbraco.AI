import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export const UAI_SHARE_PICKER_MODAL_ALIAS = "Uai.CopilotWorkspace.Modal.SharePicker";

/**
 * Mirrors the backend's `AIConversationShareScopeMode` enum `ToString()` values ("Snapshot"/"Live"),
 * sent as-is in `ShareConversationRequestModel.scopeMode` (case-insensitive server-side, but these are
 * the canonical casings the server itself returns).
 */
export type UaiConversationShareScopeMode = "Snapshot" | "Live";

export interface UaiSharePickerModalData {
    /** The conversation's title, interpolated into the `Share "<title>"` headline. Callers should
     * resolve a fallback (e.g. the "Untitled conversation" term) before passing this in — the modal
     * treats it as a plain, already-displayable string. */
    title: string;
    /**
     * Whether the conversation has any contexts/resources attached — gates the disclosure banner
     * (ARCHITECTURE.md decision 2). Callers compute this with `uaiShareHasAttachments()` from the
     * conversation's `contextIds`/`resources` rather than passing the raw arrays, so the modal doesn't
     * need to know their shape.
     */
    hasAttachments: boolean;
}

export interface UaiSharePickerModalValue {
    recipientUserKey: string;
    scopeMode: UaiConversationShareScopeMode;
}

/**
 * Single-recipient share form (one person + a mode), opened directly by the "Shared with" section's
 * "Choose" button (SC-14) — repeatable, one person per invocation. `{ type: "sidebar", size: "small" }`
 * matches Umbraco.Automate's own convention for a comparably-scoped settings-style modal (see
 * SPEC.md's `UAI_SHARE_PICKER_MODAL` section).
 *
 * This modal only resolves a value — it does not call `UaiConversationRepository.share()` itself. Its
 * caller does, after the modal resolves, the same split `UAI_RENAME_CONVERSATION_MODAL` already uses.
 */
export const UAI_SHARE_PICKER_MODAL = new UmbModalToken<UaiSharePickerModalData, UaiSharePickerModalValue>(
    UAI_SHARE_PICKER_MODAL_ALIAS,
    { modal: { type: "sidebar", size: "small" } },
);
