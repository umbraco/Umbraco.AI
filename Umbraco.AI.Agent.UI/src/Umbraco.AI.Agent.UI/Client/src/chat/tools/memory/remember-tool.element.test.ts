// MEM-11 — Memory notes in the chat (AC1-AC8, AC10-AC12, AC15, AC16)
import { describe, it } from "vitest";

// Real entry point: the `uaiAgentToolRenderer` element for `remember` rendered with a tool call's args and
// result, with the memory repository stubbed at its boundary (move / delete / keep calls).
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: remember tool note", () => {
    describe("Scenario: a fact saved to About you", () => {
        // Arrange (beforeEach): result { status: "saved", memory: { spaceType: "user" }, moveTarget }

        // AC1
        it.skip("reads Saved to About you", () => {
            // expect(text(el)).toContain("Saved to About you");
        });

        // AC1
        it.skip("shows Move and Undo buttons", () => {
            // expect(buttonLabels(el)).toEqual(["Move", "Undo"]);
        });
    });

    describe("Scenario: a lesson saved", () => {
        // Arrange (beforeEach): result { status: "saved", memory: { kind: "lesson", text } }

        // AC2
        it.skip("reads Learned with the text", () => {
            // expect(text(el)).toContain(`Learned: ${memoryText}`);
        });

        // AC2
        it.skip("shows Undo and no Move", () => {
            // expect(buttonLabels(el)).toEqual(["Undo"]);
        });
    });

    describe("Scenario: a memory saved with an end date", () => {
        // Arrange (beforeEach): result { status: "saved", memory: { expiresAt: "2027-03-01" } }

        // AC3
        it.skip("adds until 01-03-2027", () => {
            // expect(text(el)).toContain("until 01-03-2027");
        });
    });

    describe("Scenario: a memory saved replacing two", () => {
        // Arrange (beforeEach): args { replaces: [a, b] }, result { status: "saved" }

        // AC4
        it.skip("reads Updated <layer> with the text", () => {
            // expect(text(el)).toContain(`Updated ${layerLabel}: ${memoryText}`);
        });

        // AC4
        it.skip("says merged 2", () => {
            // expect(text(el)).toContain("merged 2");
        });
    });

    describe("Scenario: the user clicks Move on a saved note in a private chat", () => {
        // Arrange (beforeEach): saved note with moveTarget; repository.move resolves with the moved memory; click Move

        // AC5
        it.skip("names the other layer", () => {
            // expect(text(el)).toContain(moveTarget.label);
        });
    });

    describe("Scenario: the user clicks Undo on a saved note", () => {
        // Arrange (beforeEach): saved note; repository.delete resolves; click Undo

        // AC6
        it.skip("reads Removed", () => {
            // expect(text(el)).toContain("Removed");
        });
    });

    describe("Scenario: a held save", () => {
        // Arrange (beforeEach): result { status: "held", reason: "instruction-like" }

        // AC7
        it.skip("reads Held for you to check with the reason", () => {
            // expect(text(el)).toContain("Held for you to check"); // and the reason label
        });

        // AC7
        it.skip("shows Keep and Discard", () => {
            // expect(buttonLabels(el)).toEqual(["Keep", "Discard"]);
        });
    });

    describe("Scenario: a not_saved result", () => {
        // Arrange (beforeEach): result { status: "not_saved", reason }

        // AC8
        it.skip("reads Not saved with the reason", () => {
            // expect(text(el)).toContain(`Not saved`); // and the reason
        });

        // AC8
        it.skip("shows no buttons", () => {
            // expect(buttonLabels(el)).toEqual([]);
        });
    });

    describe("Scenario: a space save by a Work agent", () => {
        // Arrange (beforeEach): result { status: "saved", memory: { spaceType: "user-agent" } } with no moveTarget

        // AC10
        it.skip("shows no Move button", () => {
            // expect(buttonLabels(el)).not.toContain("Move");
        });
    });

    describe("Scenario: Undo on a note whose memory was already deleted", () => {
        // Arrange (beforeEach): saved note; repository.delete resolves with a 404 error; click Undo

        // AC11
        it.skip("shows the error in the note", () => {
            // expect(errorText(el)).not.toBe("");
        });

        // AC12
        it.skip("keeps the Undo button", () => {
            // expect(buttonLabels(el)).toContain("Undo");
        });
    });

    describe("Scenario: a reopened chat whose memory was moved", () => {
        // Arrange (beforeEach): saved note rendered from history; GET /{id} returns the memory in a new layer

        // AC15
        it.skip("reads the current layer from GET /{id}", () => {
            // expect(text(el)).toContain(`Saved to ${currentLayerLabel}`);
        });
    });

    describe("Scenario: a reopened chat whose memory was purged", () => {
        // Arrange (beforeEach): saved note rendered from history; GET /{id} responds 404

        // AC16
        it.skip("reads Removed", () => {
            // expect(text(el)).toContain("Removed");
        });

        // AC16
        it.skip("shows no buttons", () => {
            // expect(buttonLabels(el)).toEqual([]);
        });
    });
});
