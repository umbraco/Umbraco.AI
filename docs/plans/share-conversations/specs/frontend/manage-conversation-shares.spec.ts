// STAGED SPEC — see share-conversation.spec.ts's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-14).

import { describe, it, expect } from "vitest";

// STORIES.md US2 — See and manage who a conversation is shared with.
// Real entry point exercised: UaiConversationRepository.getShares()/revokeShare() (SPEC.md's
// GET/DELETE .../shares) as consumed by the context panel's "Shared with" section — not a modal;
// per ARCHITECTURE.md decision 7 there is no entity action or separate manage-view modal.

describe("Feature: manage conversation shares", () => {
    describe("Scenario: conversation shared with two colleagues", () => {
        it.skip("pending implementation — SC-14", () => {
            // AC1 — the "Shared with" section lists both, with scope mode, sourced from getShares(),
            // rendered first (above Contexts/Resources).
        });
    });

    describe("Scenario: conversation never shared", () => {
        it.skip("pending implementation — SC-14", () => {
            // AC2 — the section shows "Not shared with anyone yet." and a "Choose" button instead of
            // a list.
        });
    });

    describe("Scenario: removing a recipient", () => {
        it.skip("pending implementation — SC-14", () => {
            // AC3 — clicking a row's inline remove (trash) action calls
            // repository.revokeShare(conversation, userKey) and removes that row immediately, with
            // no confirm step and no further dialog — mirrors uai-resource-list's existing
            // #renderRemoveAction pattern rather than a new interaction.
        });
    });
});
