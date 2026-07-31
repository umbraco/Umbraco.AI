import { describe, expect, it } from "vitest";
import { formatCachedTokenDetail } from "./cached-token-detail.js";

/**
 * The three-way distinction here is the whole point of the field: not reported, reported as zero, and
 * reported with a value have to read differently, or a dashboard implies caching is failing when in fact
 * nobody measured it.
 */
describe("cached token detail", () => {
    it("says nothing when no provider reported a figure", () => {
        expect(formatCachedTokenDetail(12_400, undefined)).toBeUndefined();
        expect(formatCachedTokenDetail(12_400, null)).toBeUndefined();
    });

    it("distinguishes a reported zero from nothing reported", () => {
        expect(formatCachedTokenDetail(12_400, 0)).toBe("none cached");
    });

    it("reports the amount and its share of the input total", () => {
        expect(formatCachedTokenDetail(12_400, 9_800)).toBe("9.8k cached (79.0%)");
    });

    it("uses whole numbers below a thousand, matching the card above it", () => {
        expect(formatCachedTokenDetail(800, 512)).toBe("512 cached (64.0%)");
    });

    it("omits the share rather than dividing by zero", () => {
        expect(formatCachedTokenDetail(0, 512)).toBe("512 cached");
    });
});
