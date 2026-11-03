import { describe, expect, it } from "vitest";
import {
    UAI_DEFAULT_SHARE_SCOPE_MODE,
    uaiBuildShareValue,
    uaiCanSubmitShare,
    uaiShareHasAttachments,
} from "./share-picker-modal.helpers.js";
import type { ContextResourceModel } from "../types.js";

// STORIES.md US1 — Share a conversation with a colleague. Real entry point exercised: the share-picker
// modal's pure helpers — `uaiBuildShareValue` (the value the caller forwards to
// `UaiConversationRepository.share()`, per SPEC.md's `share(conversation, recipientUserKey, scopeMode)`),
// `uaiShareHasAttachments` (disclosure-banner visibility), and `uaiCanSubmitShare` (submit-disabled gate).
// This project's convention is flat Vitest over plain functions, not mounted custom elements (see
// tree-item.helpers.test.ts, types.test.ts) — no test in this project renders full shadow DOM.
//
// End-to-end "submitting calls repository.share() and closes the modal" is exercised where that call
// actually happens — the "Shared with" section built in SC-14, which is this modal's caller.

function resource(overrides: Partial<ContextResourceModel> = {}): ContextResourceModel {
    return {
        id: "55555555-5555-5555-5555-555555555555",
        resourceTypeId: "text",
        name: "Notes",
        sortOrder: 0,
        injectionMode: "Always",
        ...overrides,
    };
}

describe("Feature: share a conversation", () => {
    describe("Scenario: one recipient, Snapshot mode", () => {
        it("builds a value carrying the picked recipient and Snapshot", () => {
            // AC1
            expect(uaiBuildShareValue("user-1", "Snapshot")).toEqual({
                recipientUserKey: "user-1",
                scopeMode: "Snapshot",
            });
        });

        it("defaults the dropdown to Snapshot", () => {
            // AC1 — sharing defaults to a frozen snapshot.
            expect(UAI_DEFAULT_SHARE_SCOPE_MODE).toBe("Snapshot");
        });
    });

    describe("Scenario: Live mode", () => {
        it("builds a value carrying the picked recipient and Live", () => {
            // AC3
            expect(uaiBuildShareValue("user-1", "Live")).toEqual({
                recipientUserKey: "user-1",
                scopeMode: "Live",
            });
        });
    });

    describe("Scenario: re-sharing to an already-shared recipient", () => {
        it("builds the same value both times — no client-side de-duplication or blocking", () => {
            // AC4 — the modal doesn't need special-case UI for this; the backend upserts (SC-04). This
            // spec covers that the modal's own value-building doesn't block or alter a repeat submission.
            const first = uaiBuildShareValue("user-1", "Snapshot");
            const second = uaiBuildShareValue("user-1", "Snapshot");
            expect(second).toEqual(first);
        });
    });

    describe("Scenario: conversation has attached resources/contexts", () => {
        it("flags attachments present when contextIds is non-empty", () => {
            // AC5
            expect(uaiShareHasAttachments(["context-1"], [])).toBe(true);
        });

        it("flags attachments present when resources is non-empty", () => {
            // AC5
            expect(uaiShareHasAttachments([], [resource()])).toBe(true);
        });
    });

    describe("Scenario: conversation has no attachments", () => {
        it("flags no attachments when both contextIds and resources are empty", () => {
            // AC6, negative case
            expect(uaiShareHasAttachments([], [])).toBe(false);
        });
    });

    describe("Scenario: no recipient picked yet", () => {
        it("disables submit when no recipient key is set", () => {
            // AC10
            expect(uaiCanSubmitShare(undefined)).toBe(false);
        });

        it("enables submit once a recipient key is set", () => {
            // AC10
            expect(uaiCanSubmitShare("user-1")).toBe(true);
        });
    });
});
