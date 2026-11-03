import type { ConversationShareResponseModel, UaiConversationDetailModel } from "../types.js";

/**
 * The conversation id the "Shared with" section should fetch `GET .../shares` for, or `undefined`
 * when there's nothing to show: a recipient's read-only shared view (`GET .../shares` is owner-only),
 * an archived conversation (also `readonly`), or an unsaved draft (no id yet). Pulled out as a pure
 * function so the panel's gating logic — "owner's own persisted, non-readonly conversation only" —
 * is independently testable without mounting the element.
 */
export function uaiSharesConversationId(conversation: UaiConversationDetailModel | undefined): string | undefined {
    return conversation && !conversation.readonly ? conversation.id : undefined;
}

/**
 * Stable sort for the "Shared with" list — by recipient display name — so the list order doesn't
 * reshuffle on every refetch as server-side ordering or `dateShared` ties vary. `GET .../shares`
 * doesn't document a required order (SPEC.md), so this is the section's own choice.
 */
export function uaiSortShares(shares: ConversationShareResponseModel[]): ConversationShareResponseModel[] {
    return [...shares].sort((a, b) => a.userName.localeCompare(b.userName));
}

/**
 * Optimistically drops a revoked recipient from the locally held list so the row disappears
 * immediately on click, without waiting for a refetch.
 */
export function uaiRemoveShare(
    shares: ConversationShareResponseModel[],
    userKey: string,
): ConversationShareResponseModel[] {
    return shares.filter((s) => s.userKey !== userKey);
}
