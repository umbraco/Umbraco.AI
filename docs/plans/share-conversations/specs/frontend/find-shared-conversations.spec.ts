// STAGED SPEC — see share-conversation.spec.ts's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-13).
// Sits alongside grouping.test.ts's existing groupConversations() specs — the "Shared with me" group
// is expected to be added to that same grouping module/helper per SPEC.md's sidebar section.

import { describe, it, expect } from "vitest";

// STORIES.md US3 — Find conversations shared with me.
// Real entry point exercised: the sidebar grouping helper that turns
// requestSharedWithMe()'s result into the rendered "Shared with me" group, and the entity-action
// registry's scoping of Pin/Archive/Rename/Move/Delete away from shared entries.

describe("Feature: find shared conversations", () => {
    describe("Scenario: a colleague has shared a conversation with me", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC1 — it appears under a distinct "Shared with me" group, with title and owner name.
        });
    });

    describe("Scenario: a share was revoked", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC2 — no longer present in the grouped result once requestSharedWithMe() stops
            // returning it (backend already covered by FindSharedConversationsTests.cs).
        });
    });

    describe("Scenario: a shared entry's available entity actions", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC3 — Pin/Archive/Rename/Move/Delete are not offered for a shared conversation; only
            // opening it is.
        });
    });
});
