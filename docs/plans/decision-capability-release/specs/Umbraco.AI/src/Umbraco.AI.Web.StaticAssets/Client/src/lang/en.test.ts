// DR-7 — Manage Decision profiles in the backoffice (AC1)
import { describe, expect, it } from "vitest";
import en from "./en.js";

/**
 * The create-profile modal labels each capability via `uaiCapabilities_<lowercase>`. A missing key
 * renders the raw key (see #413), so the label is asserted at its source.
 */
describe("Feature: capability labels", () => {
    describe("Scenario: the Decision capability is offered when creating a profile", () => {
        // Pending T16
        it.skip('labels it "Decision"', () => {
            expect((en.uaiCapabilities as Record<string, string>).decision).toBe("Decision");
        });
    });
});
