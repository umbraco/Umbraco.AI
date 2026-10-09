import type { UaiModelFactModel } from "../../../connection/types.js";

/** The most facts shown under the Model field. */
export const MAX_DISPLAYED_FACTS = 6;

/**
 * Facts to render, in render order: Warning facts first, otherwise the server's order, capped.
 * Warnings are ranked before the cap so one past the cut-off still shows.
 */
export function selectDisplayFacts(facts: readonly UaiModelFactModel[]): UaiModelFactModel[] {
    const warnings = facts.filter((fact) => fact.tone === "Warning");
    const others = facts.filter((fact) => fact.tone !== "Warning");
    return [...warnings, ...others].slice(0, MAX_DISPLAYED_FACTS);
}

/** True only for an absolute http/https URL. */
export function isSafeFactUrl(url: string | null | undefined): boolean {
    if (!url) return false;
    try {
        const { protocol } = new URL(url);
        return protocol === "http:" || protocol === "https:";
    } catch {
        return false;
    }
}
