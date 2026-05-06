/**
 * Active variant selection helpers shared across entity adapters.
 *
 * Adapters group `getValues()` entries by alias and use these helpers to pick
 * the value that matches the variant the editor currently has focused — so
 * prompt template variables on multi-variant content resolve to the active
 * culture's value.
 */

/**
 * Identifies the variant the editor is currently focused on.
 * Sourced per adapter from the underlying workspace context (e.g.
 * `splitView.getActiveVariants()` for documents, `getVariantId()` for blocks).
 */
export interface ActiveVariantInfo {
    culture: string | null;
    segment: string | null;
}

/**
 * Pick the value entry that matches the active variant.
 *
 * Resolution order:
 * 1. Exact `(culture, segment)` match against the active variant.
 * 2. Invariant entry (`culture: null, segment: null`) — covers properties that
 *    don't vary on this content type.
 * 3. Last entry — preserves the pre-fix behaviour for payloads without
 *    culture metadata (the previous Map-based code was last-write-wins, so
 *    the last entry won implicitly).
 *
 * Mirrors `PickValueForVariant` in `AIEntityContextHelper` so the client and
 * server agree on which value resolves for a given alias.
 */
export function pickValueForVariant<T extends { culture: string | null; segment: string | null }>(
    entries: T[],
    active: ActiveVariantInfo | null,
): T | undefined {
    if (entries.length === 0) return undefined;

    if (active) {
        const exact = entries.find((e) => e.culture === active.culture && e.segment === active.segment);
        if (exact) return exact;
    }

    const invariant = entries.find((e) => e.culture === null && e.segment === null);
    if (invariant) return invariant;

    return entries[entries.length - 1];
}
