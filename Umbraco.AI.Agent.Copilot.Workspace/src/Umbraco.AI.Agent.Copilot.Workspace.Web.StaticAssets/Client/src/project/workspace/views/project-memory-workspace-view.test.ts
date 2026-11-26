// MEM-14 — Project memory (AC3-AC7)
import { describe, it } from "vitest";

// Real entry point: the project Memory workspace view (T25) and the helpers it renders from, with the
// Agent package's memory repository stubbed at its boundary.
// This project's convention is flat Vitest over the view's pure helpers (see
// conversation-context-panel.helpers.test.ts); the builder may instead render the element if the harness allows.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: project memory tab", () => {
    describe("Scenario: a project with memories", () => {
        // Arrange (beforeEach): repository returns three active memories for the project

        // AC3
        it.skip("lists them with Kind, Status, Until, Saved by and Saved columns", () => {
            // expect(columnHeaders(el)).toEqual(["Memory", "Kind", "Status", "Until", "Saved by", "Saved", ""]);
        });
    });

    describe("Scenario: a project with one held memory and five active ones", () => {
        // Arrange (beforeEach): repository returns 5 active then 1 held

        // AC4
        it.skip("lists the held row first", () => {
            // expect(rows[0].status).toBe("held");
        });

        // AC5
        it.skip("badges the tab with 1", () => {
            // expect(badgeCount).toBe(1);
        });
    });

    describe("Scenario: a project memory saved by a deleted user", () => {
        // Arrange (beforeEach): repository returns a memory with createdBy: null

        // AC6
        it.skip("shows Saved by as A deleted user", () => {
            // expect(rows[0].savedBy).toBe("A deleted user");
        });
    });

    describe("Scenario: a project with no memories", () => {
        // Arrange (beforeEach): repository returns []

        // AC7
        it.skip("shows the empty state about saving facts about the project as you chat", () => {
            // expect(emptyStateText(el)).toContain("saves facts about the project as you chat");
        });
    });
});
