// DR-7 — Manage Decision profiles in the backoffice (AC2)
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import "./decision-profile-settings.element.js";

describe("Feature: Decision profile settings view", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: a Decision profile is opened", () => {
        let el: HTMLElement & { updateComplete: Promise<unknown> };

        beforeEach(async () => {
            el = document.createElement("uai-decision-profile-settings") as typeof el;
            (el as unknown as { settings: unknown }).settings = { $type: "decision" };
            document.body.appendChild(el);
            await el.updateComplete;
        });

        // Pending T16
        it.skip("renders a message instead of a blank area", () => {
            expect(el.shadowRoot!.textContent?.trim().length).toBeGreaterThan(0);
        });
    });
});
