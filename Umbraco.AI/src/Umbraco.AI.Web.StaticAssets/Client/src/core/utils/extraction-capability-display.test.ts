// EX-5 — Manage Extraction profiles in the backoffice (AC1)
import { describe, expect, it } from "vitest";
import en from "../../lang/en.js";
import { getCapabilityIcon } from "./capability-icon.utils.js";

/**
 * Every place a capability is listed (connection capabilities, profile create, usage dashboards)
 * reads its label from `uaiCapabilities_<lowercase>` and its icon from `getCapabilityIcon`, so both
 * are asserted at their source, like the Decision label spec in `lang/en.test.ts`.
 */
describe("Feature: Extraction capability label and icon", () => {
    describe("Scenario: the Extraction capability is listed", () => {
        it('labels it "Extraction"', () => {
            expect((en.uaiCapabilities as Record<string, string>).extraction).toBe("Extraction");
        });

        it("gives it the icon-filter icon", () => {
            expect(getCapabilityIcon("extraction")).toBe("icon-filter");
        });

        it("matches the icon whatever the casing of the capability name", () => {
            expect(getCapabilityIcon("Extraction")).toBe("icon-filter");
        });

        it("keeps every known capability's icon distinct", () => {
            const capabilities = [
                "chat",
                "embedding",
                "media",
                "moderation",
                "speechtotext",
                "imagegeneration",
                "decision",
                "extraction",
            ];
            expect(new Set(capabilities.map(getCapabilityIcon)).size).toBe(capabilities.length);
        });
    });
});
