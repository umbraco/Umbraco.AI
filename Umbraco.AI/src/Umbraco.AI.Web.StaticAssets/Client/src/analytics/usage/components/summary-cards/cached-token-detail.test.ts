import { describe, expect, it } from "vitest";
import { formatCachedTokenDetail } from "./cached-token-detail.js";

/**
 * The three-way distinction here is the whole point of the field: not reported, reported as zero, and
 * reported with a value have to read differently, or a dashboard implies caching is failing when in fact
 * nobody measured it.
 */
describe("cached token detail", () => {
    it("says nothing when no provider reported a figure", () => {
        expect(formatCachedTokenDetail(undefined)).toBeUndefined();
        expect(formatCachedTokenDetail(null)).toBeUndefined();
    });

    it("distinguishes a reported zero from nothing reported", () => {
        expect(formatCachedTokenDetail(0)).toBe("none cached");
    });

    it("reports the amount in the same units as the value it sits beside", () => {
        expect(formatCachedTokenDetail(14_300)).toBe("14.3k cached");
    });

    it("uses whole numbers below a thousand, matching the card above it", () => {
        expect(formatCachedTokenDetail(512)).toBe("512 cached");
    });
});
