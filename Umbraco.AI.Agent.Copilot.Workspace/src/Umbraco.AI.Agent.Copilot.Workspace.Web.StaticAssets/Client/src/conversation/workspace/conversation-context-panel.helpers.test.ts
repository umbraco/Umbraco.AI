import { describe, expect, it } from "vitest";
import { uaiRemoveShare, uaiSharesConversationId, uaiSortShares } from "./conversation-context-panel.helpers.js";
import type { ConversationShareResponseModel, UaiConversationDetailModel } from "../types.js";

// STORIES.md US2 — See and manage who a conversation is shared with. Real entry point exercised:
// the "Shared with" section's pure helpers — `uaiSharesConversationId` (the owner-only fetch gate),
// `uaiSortShares` (the list's stable sort), and `uaiRemoveShare` (the row's optimistic removal on
// revoke) — the section built in SC-14 calls these around `UaiConversationRepository.getShares()` /
// `revokeShare()`. This project's convention is flat Vitest over plain functions, not mounted custom
// elements (see tree-item.helpers.test.ts, share-picker-modal.helpers.test.ts) — no test in this
// project renders full shadow DOM.

function conversation(overrides: Partial<UaiConversationDetailModel> = {}): UaiConversationDetailModel {
    return {
        id: "11111111-1111-1111-1111-111111111111",
        projectId: null,
        title: "Debugging the checkout redirect",
        agentIdOrAlias: null,
        profileId: null,
        contextIds: [],
        resources: [],
        isPinned: false,
        isArchived: false,
        isShared: false,
        readonly: false,
        ...overrides,
    };
}

function share(overrides: Partial<ConversationShareResponseModel> = {}): ConversationShareResponseModel {
    return {
        userKey: "user-1",
        userName: "Sofie Jensen",
        scopeMode: "Live",
        dateShared: "2026-10-01T00:00:00Z",
        ...overrides,
    };
}

describe("Feature: manage conversation shares", () => {
    describe("Scenario: conversation shared with two colleagues", () => {
        it("sorts the list by recipient name", () => {
            // AC1 — the "Shared with" section lists both, sourced from getShares().
            const shares = [
                share({ userKey: "user-2", userName: "Zara Khan" }),
                share({ userKey: "user-1", userName: "Anders Kristensen" }),
            ];
            const sorted = uaiSortShares(shares);
            expect(sorted.map((s) => s.userName)).toEqual(["Anders Kristensen", "Zara Khan"]);
        });

        it("does not mutate the array passed in", () => {
            // AC1 — the section re-sorts on every fetch without the caller losing its own copy.
            const shares = [
                share({ userKey: "user-2", userName: "Zara Khan" }),
                share({ userKey: "user-1", userName: "Anders Kristensen" }),
            ];
            uaiSortShares(shares);
            expect(shares[0]?.userName).toBe("Zara Khan");
        });
    });

    describe("Scenario: owner's own, persisted, non-readonly conversation", () => {
        it("fetches shares for its id", () => {
            // AC1/AC2 — the section only has anything to show for the owner's own conversations.
            expect(uaiSharesConversationId(conversation())).toBe("11111111-1111-1111-1111-111111111111");
        });
    });

    describe("Scenario: conversation is a recipient's read-only shared view", () => {
        it("does not fetch shares", () => {
            // Readonly covers both an archived conversation and a recipient's shared view — `GET
            // .../shares` is owner-only, so there's nothing to show here regardless.
            expect(uaiSharesConversationId(conversation({ readonly: true }))).toBeUndefined();
        });
    });

    describe("Scenario: conversation is still an unsaved draft", () => {
        it("does not fetch shares", () => {
            // A draft has no id yet, so there's nothing to fetch.
            expect(uaiSharesConversationId(conversation({ id: undefined }))).toBeUndefined();
        });
    });

    describe("Scenario: nothing open yet", () => {
        it("does not fetch shares", () => {
            expect(uaiSharesConversationId(undefined)).toBeUndefined();
        });
    });

    describe("Scenario: conversation never shared", () => {
        it("sorts an empty list to an empty list", () => {
            // AC2 — the section shows "Not shared with anyone yet." in place of the list.
            expect(uaiSortShares([])).toEqual([]);
        });
    });

    describe("Scenario: removing a recipient", () => {
        it("drops just that recipient from the held list", () => {
            // AC3 — clicking a row's inline remove (trash) action removes that row immediately, with
            // no confirm step, mirroring uai-resource-list's existing #renderRemoveAction pattern.
            const shares = [share({ userKey: "user-1" }), share({ userKey: "user-2" })];
            const result = uaiRemoveShare(shares, "user-1");
            expect(result.map((s) => s.userKey)).toEqual(["user-2"]);
        });

        it("leaves the list unchanged when the key isn't present", () => {
            const shares = [share({ userKey: "user-1" })];
            const result = uaiRemoveShare(shares, "user-2");
            expect(result).toEqual(shares);
        });
    });
});
