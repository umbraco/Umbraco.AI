import { describe, expect, it } from "vitest";
import { getCapabilityIcon } from "./capability-icon.utils.js";

/**
 * Every capability the frontend knows about (`uaiCapabilities` in src/lang/en.ts) should render
 * with its own icon -- a shared fallback (e.g. everything mapping to icon-wand) makes capabilities
 * indistinguishable from one another in pickers and create-flows.
 */
describe("getCapabilityIcon", () => {
    it("gives every known capability a distinct icon", () => {
        const capabilities = ["chat", "embedding", "media", "moderation", "speechtotext", "imagegeneration", "decision"];
        const icons = capabilities.map(getCapabilityIcon);

        expect(new Set(icons).size).toBe(capabilities.length);
    });
});
