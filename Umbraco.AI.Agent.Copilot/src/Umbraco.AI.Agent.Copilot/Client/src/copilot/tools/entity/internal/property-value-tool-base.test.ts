import { describe, it, expect } from "vitest";
import { normalizeVariantForProperty } from "./property-value-tool-base.js";

describe("normalizeVariantForProperty", () => {
    const active = { culture: "pt-PT", segment: null };

    it("nulls culture/segment for an invariant property (the bug this fixes)", () => {
        expect(normalizeVariantForProperty(active, false, false)).toEqual({
            culture: null,
            segment: null,
        });
    });

    it("keeps the culture for a culture-variant property", () => {
        expect(normalizeVariantForProperty(active, true, false)).toEqual({
            culture: "pt-PT",
            segment: null,
        });
    });

    it("keeps the segment (and nulls culture) for a segment-only property", () => {
        expect(
            normalizeVariantForProperty({ culture: "pt-PT", segment: "seg-1" }, false, true),
        ).toEqual({ culture: null, segment: "seg-1" });
    });

    it("keeps both for a culture+segment property", () => {
        expect(
            normalizeVariantForProperty({ culture: "pt-PT", segment: "seg-1" }, true, true),
        ).toEqual({ culture: "pt-PT", segment: "seg-1" });
    });

    it("returns nulls for an invariant property when no variant is supplied", () => {
        expect(normalizeVariantForProperty(undefined, false, false)).toEqual({
            culture: null,
            segment: null,
        });
    });
});
