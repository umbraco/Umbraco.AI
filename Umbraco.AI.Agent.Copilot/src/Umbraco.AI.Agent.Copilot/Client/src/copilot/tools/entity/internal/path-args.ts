import type {
    PropertyPathSegment,
    VariantId,
} from "./property-value-operation.client.js";

/**
 * Parses the LLM-supplied `path` argument into the typed alternation expected by the dispatcher
 * (property aliases at even indices, block selectors at odd indices). Returns `null` when the
 * shape is invalid.
 */
export function parsePath(input: unknown): PropertyPathSegment[] | null {
    if (!Array.isArray(input) || input.length === 0) {
        return null;
    }

    const result: PropertyPathSegment[] = [];
    for (let i = 0; i < input.length; i++) {
        const segment = input[i];
        if (i % 2 === 0) {
            if (typeof segment !== "string" || segment.length === 0) {
                return null;
            }
            result.push(segment);
        } else {
            if (
                typeof segment !== "object" ||
                segment === null ||
                typeof (segment as { blockKey?: unknown }).blockKey !== "string"
            ) {
                return null;
            }
            result.push({ blockKey: (segment as { blockKey: string }).blockKey });
        }
    }

    return result;
}

/**
 * Extracts an optional culture/segment variant from tool args. Returns `undefined` when neither
 * field is supplied so the dispatcher can fall back to the active variant.
 */
export function readVariant(args: Record<string, unknown>): VariantId | undefined {
    const culture = typeof args.culture === "string" ? args.culture : null;
    const segment = typeof args.segment === "string" ? args.segment : null;
    if (culture === null && segment === null) {
        return undefined;
    }
    return { culture, segment };
}
