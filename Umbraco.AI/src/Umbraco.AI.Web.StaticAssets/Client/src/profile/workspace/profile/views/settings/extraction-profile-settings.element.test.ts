// EX-5 — Manage Extraction profiles in the backoffice (AC2)
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import "./extraction-profile-settings.element.js";
import { isExtractionProfileSettings } from "../../../../types.js";

describe("Feature: Extraction profile settings view", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: an Extraction profile is opened", () => {
        let el: HTMLElement & { updateComplete: Promise<unknown> };

        beforeEach(async () => {
            el = document.createElement("uai-extraction-profile-settings") as typeof el;
            (el as unknown as { settings: unknown }).settings = { $type: "extraction" };
            document.body.appendChild(el);
            await el.updateComplete;
        });

        it("renders a message instead of a blank area", () => {
            expect(el.shadowRoot!.textContent?.trim().length).toBeGreaterThan(0);
        });

        it("renders no inputs", () => {
            expect(
                el.shadowRoot!.querySelectorAll("uui-input, uui-select, uui-toggle, umb-property-layout").length,
            ).toBe(0);
        });
    });

    describe("Scenario: profile settings are recognised as Extraction settings", () => {
        it("accepts settings with $type extraction", () => {
            expect(isExtractionProfileSettings({ $type: "extraction" } as never)).toBe(true);
        });
    });

    describe("Sad path", () => {
        describe("Scenario: settings of another capability", () => {
            it("rejects Decision settings", () => {
                expect(isExtractionProfileSettings({ $type: "decision" } as never)).toBe(false);
            });
        });
    });
});
