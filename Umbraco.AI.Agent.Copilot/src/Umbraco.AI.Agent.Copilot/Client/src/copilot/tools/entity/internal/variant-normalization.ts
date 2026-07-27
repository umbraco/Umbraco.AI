import type { VariantId } from "./property-value-operation.client.js";

/**
 * Normalises a staged variant against a property's variance. An invariant property must be staged
 * with `culture`/`segment` of `null`; otherwise the save is rejected by the Management API with
 * `PropertyTypeCultureVarianceMismatch`. Genuinely variant properties keep their culture/segment.
 *
 * Kept in its own module (no CMS/runtime imports) so it stays trivially unit-testable.
 */
export function normalizeVariantForProperty(
    variant: VariantId | undefined,
    variesByCulture: boolean,
    variesBySegment: boolean,
): VariantId {
    return {
        culture: variesByCulture ? (variant?.culture ?? null) : null,
        segment: variesBySegment ? (variant?.segment ?? null) : null,
    };
}
