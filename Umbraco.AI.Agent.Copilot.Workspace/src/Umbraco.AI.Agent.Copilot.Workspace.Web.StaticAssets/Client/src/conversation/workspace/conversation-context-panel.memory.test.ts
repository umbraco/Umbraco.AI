// MEM-15 — Shared chat memory (AC4, AC5, AC8, AC11)
import { describe, it } from "vitest";

// Real entry point: the Memory section of the conversation context panel (T26) and the helpers it
// renders from, with the memory repository stubbed at its boundary.
// This project's convention is flat Vitest over the view's pure helpers (see
// conversation-context-panel.helpers.test.ts); the builder may instead render the element if the harness allows.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: shared chat memory section", () => {
    describe("Scenario: a shared chat with memories, opened by the owner", () => {
        // Arrange (beforeEach): conversation isShared (ever shared), repository returns two memories

        // AC4
        it.skip("shows a Memory section after Shared with listing them", () => {
            // expect(sectionHeadings(el)).toEqual([...before, "Shared with", "Memory"]); // with the two rows
        });
    });

    describe("Scenario: a chat that was never shared", () => {
        // Arrange (beforeEach): conversation never shared

        // AC5
        it.skip("shows no Memory section", () => {
            // expect(sectionHeadings(el)).not.toContain("Memory");
        });
    });

    describe("Scenario: a shared chat with no memories", () => {
        // Arrange (beforeEach): conversation shared, repository returns []

        // AC8
        it.skip("reads Nothing remembered in this chat yet", () => {
            // expect(memorySectionText(el)).toBe("Nothing remembered in this chat yet");
        });
    });

    describe("Scenario: a chat shared as a Snapshot, opened by the recipient", () => {
        // Arrange (beforeEach): conversation opened by a recipient of a Snapshot share

        // AC11
        it.skip("shows no Memory section", () => {
            // expect(sectionHeadings(el)).not.toContain("Memory");
        });
    });
});
