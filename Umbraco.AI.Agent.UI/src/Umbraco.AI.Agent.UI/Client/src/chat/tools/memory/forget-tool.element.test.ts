// MEM-11 — Memory notes in the chat (AC9, AC13, AC14)
import { describe, it } from "vitest";

// Real entry point: the `uaiAgentToolRenderer` element for `forget` rendered with a tool call's result.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: forget tool note", () => {
    describe("Scenario: a forgotten result", () => {
        // Arrange (beforeEach): result { status: "forgotten", memory: { text } }

        // AC9
        it.skip("reads Forgot with the text", () => {
            // expect(text(el)).toContain(`Forgot: ${memoryText}`);
        });
    });

    describe("Scenario: a forgotten result offers Undo", () => {
        // Arrange (beforeEach): result { status: "forgotten", memory: { id, text } }

        // AC13
        it.skip("shows an Undo button", () => {
            // expect(buttonLabels(el)).toContain("Undo");
        });
    });

    describe("Scenario: the user clicks Undo on a forgotten note", () => {
        // Arrange (beforeEach): forgotten note; repository.restore resolves; click Undo

        // AC14
        it.skip("calls POST /{id}/restore for the memory", () => {
            // expect(repository.restore).toHaveBeenCalledWith(memoryId);
        });

        // AC14
        it.skip("reads Restored", () => {
            // expect(text(el)).toContain("Restored");
        });
    });
});
