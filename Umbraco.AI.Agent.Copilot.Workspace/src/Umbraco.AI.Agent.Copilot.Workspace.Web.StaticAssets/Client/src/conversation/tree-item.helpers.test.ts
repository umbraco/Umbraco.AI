import { describe, it, expect } from "vitest";
import { conversationIconName, conversationSignFlags } from "./tree-item.helpers.js";
import { UAI_CONVERSATION_SHARED_FLAG } from "../constants.js";
import type { ConversationResponseModel } from "./types.js";

// STORIES.md US2 AC5-7 — the owner's own conversation rows show a real Umbraco entity-sign badge when
// the conversation has active shares (ARCHITECTURE.md decision 6), not an icon swap. Real entry point
// exercised: the entitySign manifest's `forEntityFlags` match against `hasActiveShares`-derived flags,
// computed here by `conversationSignFlags`/`conversationIconName` exactly as `render()` calls them.
//
// AC7 (hovering the badge shows the "Shared" label as a tooltip only, nothing opens/navigates) is not
// covered by its own test here: the entitySign manifest is registered with `kind: "icon"` (see
// manifests.ts), a pure CSS/data construct provided entirely by core's `<umb-entity-sign-bundle>` — this
// package adds no click handler or navigation of its own to the badge, so there is no package-owned
// behavior to assert against beyond the manifest registration itself.

function conv(overrides: Partial<ConversationResponseModel> = {}): ConversationResponseModel {
    return {
        id: "11111111-1111-1111-1111-111111111111",
        projectId: null,
        title: "Untitled",
        agentIdOrAlias: null,
        profileId: null,
        contextIds: [],
        resources: [],
        isPinned: false,
        isArchived: false,
        hasActiveShares: false,
        dateCreated: "2026-07-29T12:00:00Z",
        dateModified: "2026-07-29T12:00:00Z",
        lastMessageAt: null,
        ...overrides,
    } as ConversationResponseModel;
}

describe("Feature: owner-side shared indicator", () => {
    describe("Scenario: conversation has an active share", () => {
        it("passes the shared entity flag when hasActiveShares is true", () => {
            // AC5
            expect(conversationSignFlags(conv({ hasActiveShares: true }))).toEqual([
                { alias: UAI_CONVERSATION_SHARED_FLAG },
            ]);
        });

        it("passes no entity flags when hasActiveShares is false", () => {
            // AC5
            expect(conversationSignFlags(conv({ hasActiveShares: false }))).toEqual([]);
        });
    });

    describe("Scenario: conversation is both pinned and shared", () => {
        it("keeps the pushpin icon while also carrying the shared flag", () => {
            // AC6 — the main icon stays the pushpin (unchanged pin-vs-chat logic); the entity-sign badge
            // is driven by a separate, independent flags input, so both are present simultaneously. This
            // is the one behavior the superseded icon-swap design (ARCHITECTURE.md decision 6) could not do.
            const conversation = conv({ isPinned: true, hasActiveShares: true });

            expect(conversationIconName(conversation)).toBe("icon-pushpin");
            expect(conversationSignFlags(conversation)).toEqual([{ alias: UAI_CONVERSATION_SHARED_FLAG }]);
        });
    });

    describe("Scenario: conversation is unpinned", () => {
        it("uses the chat icon", () => {
            expect(conversationIconName(conv({ isPinned: false }))).toBe("icon-chat");
        });
    });
});
