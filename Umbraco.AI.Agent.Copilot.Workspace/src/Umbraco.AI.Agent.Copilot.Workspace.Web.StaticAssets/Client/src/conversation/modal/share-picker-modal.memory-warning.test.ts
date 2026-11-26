// MEM-14 — Project memory (AC10)
import { describe, it } from "vitest";

// Real entry point: the share picker modal's warning (share-picker-modal.helpers.ts / element, T26).
// This project's convention is flat Vitest over the view's pure helpers (see
// conversation-context-panel.helpers.test.ts); the builder may instead render the element if the harness allows.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: share warning mentions memory", () => {
    describe("Scenario: the owner shares a chat in a project with memories", () => {
        // Arrange (beforeEach): conversation in a project with memories

        // AC10
        it.skip("warns that the recipient will see replies shaped by the owner's memory", () => {
            // expect(warningText).toContain("replies shaped by your memory"); // exact wording is the builder's
        });
    });
});
