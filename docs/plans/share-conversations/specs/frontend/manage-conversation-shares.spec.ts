// STAGED SPEC — see share-conversation.spec.ts's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-11).

import { describe, it, expect } from "vitest";

// STORIES.md US2 — Manage who a conversation is shared with.
// Real entry point exercised: UaiConversationRepository.getShares()/revokeShare() (SPEC.md's
// GET/DELETE .../shares) as consumed by the Share modal's current-recipients list.

describe("Feature: manage conversation shares", () => {
    describe("Scenario: conversation shared with two colleagues", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC1 — the modal lists both, with scope and date, sourced from getShares().
        });
    });

    describe("Scenario: revoking a recipient", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC2 — clicking revoke calls repository.revokeShare(conversation, userKey) and removes
            // that row from the modal's list without a full reopen.
        });
    });
});
