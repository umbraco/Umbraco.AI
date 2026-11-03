import type { ContextResourceModel } from "../types.js";
import type { UaiConversationShareScopeMode, UaiSharePickerModalValue } from "./share-picker-modal.token.js";

/** Sharing defaults to a frozen snapshot (STORIES.md AC1) — the mode the dropdown opens with. */
export const UAI_DEFAULT_SHARE_SCOPE_MODE: UaiConversationShareScopeMode = "Snapshot";

/**
 * Whether the conversation has anything that would be exposed to the recipient beyond the chat text
 * itself — gates the share-picker modal's disclosure banner (ARCHITECTURE.md decision 2). Pulled out as
 * a pure function so both the modal's caller (which builds `UaiSharePickerModalData`) and this test suite
 * share the one rule, instead of it being duplicated or only checkable by mounting the modal.
 */
export function uaiShareHasAttachments(contextIds: string[], resources: ContextResourceModel[]): boolean {
    return contextIds.length > 0 || resources.length > 0;
}

/** The "Share" submit button is enabled only once a recipient has been picked (AC10). */
export function uaiCanSubmitShare(recipientUserKey: string | undefined): boolean {
    return !!recipientUserKey;
}

/**
 * Builds the modal's resolved value. A pure function (rather than inlined in the submit handler) so the
 * shape sent for each mode — and the fact that submitting twice with the same recipient produces the
 * same value rather than being blocked or deduplicated client-side (AC4 — re-sharing is the backend's
 * upsert to handle, not this modal's) — is independently testable without mounting the modal.
 */
export function uaiBuildShareValue(
    recipientUserKey: string,
    scopeMode: UaiConversationShareScopeMode,
): UaiSharePickerModalValue {
    return { recipientUserKey, scopeMode };
}
