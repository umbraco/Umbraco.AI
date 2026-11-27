// MEM-12 (AC15) — Your AI memory: the site-off note
import { describe, it } from "vitest";

// Real entry point: the my-memory-user-profile-app custom element rendered through its public
// properties, with a stubbed GET /mine response. Pending: each body is the intended arrange/act/assert
// as comments, so this file compiles before the element exists.
describe("Feature: Your AI memory", () => {
    describe("Scenario: the site's Agent Memory switch is off", () => {
        // Given the GET /mine response holds siteMemoryEnabled: false
        // When the user opens "Your AI"
        it.skip("pending: AC15 shows the note 'Your admin has turned off agent memory'", () => {
            // expect(element.shadowRoot?.textContent).toContain("Your admin has turned off agent memory");
        });
    });
});
