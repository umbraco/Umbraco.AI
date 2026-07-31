/**
 * Formats a compact integer, e.g. 9800 -> "9.8k". Mirrors the summary card's own formatting so the detail
 * line reads in the same units as the value above it.
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
 * Describes how much of an input token total was served from a provider's prompt cache, for the line under
 * the Input Tokens card.
 *
 * Returns undefined when nothing reported a figure, which hides the line entirely — that is a different
 * state from a reported zero, and the two must not read the same. An install whose providers do not track
 * caching has nothing to say; one that does and cached nothing says so.
 */
export function formatCachedTokenDetail(inputTokens: number, cachedInputTokens?: number | null): string | undefined {
    if (cachedInputTokens === undefined || cachedInputTokens === null) {
        return undefined;
    }

    if (cachedInputTokens === 0) {
        return "none cached";
    }

    // The share is omitted rather than shown as a division by zero. Reachable in principle only if a
    // provider reports cache reads without any input total, but a NaN on the dashboard is not worth risking.
    const share = inputTokens > 0 ? ` (${((cachedInputTokens / inputTokens) * 100).toFixed(1)}%)` : "";

    return `${formatIntWithK(cachedInputTokens)} cached${share}`;
}
