/**
 * Formats a compact integer, e.g. 9800 -> "9.8k". Mirrors the summary card's own formatting so the suffix
 * reads in the same units as the value it sits beside.
 */
export function formatIntWithK(value: number, decimals: number = 1): string {
    const abs = Math.abs(value);

    if (abs < 1000) {
        return value.toString();
    }

    const formatted = (abs / 1000).toFixed(decimals);
    const clean = formatted.replace(/\.0+$/, "");

    return `${value < 0 ? "-" : ""}${clean}k`;
}

/**
 * Describes how much of an input token total was served from a provider's prompt cache, for the smaller
 * addition on the Input Tokens card's value line (e.g. "28.8k / 14.3k cached").
 *
 * Returns undefined when nothing reported a figure, which drops the suffix entirely - that is a different
 * state from a reported zero, and the two must not read the same. An install whose providers do not track
 * caching has nothing to say; one that does and cached nothing says so.
 *
 * No share percentage: the suffix shares a line with the value it qualifies, and the two figures already
 * give the reader the ratio. The breakdown table is where per-dimension detail belongs.
 */
export function formatCachedTokenDetail(cachedInputTokens?: number | null): string | undefined {
    if (cachedInputTokens === undefined || cachedInputTokens === null) {
        return undefined;
    }

    if (cachedInputTokens === 0) {
        return "none cached";
    }

    return `${formatIntWithK(cachedInputTokens)} cached`;
}
