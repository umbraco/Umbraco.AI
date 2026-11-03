// STAGED SPEC — see share-conversation.spec.ts's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-17).
// Sits alongside grouping.test.ts's existing groupConversations() specs — the "Shared with me" group
// is expected to be added to that same grouping module/helper per SPEC.md's sidebar section.

import { describe, it, expect } from "vitest";

// STORIES.md US3 — Find conversations shared with me.
// Real entry point exercised: the sidebar grouping helper that turns
// requestSharedWithMe()'s result into the rendered "Shared with me" group (plain icon + the same
// entity-sign badge treatment as SC-13's owner-side rows, but with entityFlags set unconditionally
// rather than from a hasActiveShares field — see PLAN.md SC-17), and the entity-action registry's
// scoping of Pin/Archive/Rename/Move/Delete away from shared entries.

describe("Feature: find shared conversations", () => {
    describe("Scenario: a colleague has shared a conversation with me", () => {
        it.skip("pending implementation — SC-17", () => {
            // AC1 — it appears under a distinct "Shared with me" group, as a single line (title
            // only) with the icon + badge, no owner-name subtitle.
        });
    });

    describe("Scenario: hovering the badge on a shared-with-me entry", () => {
        it.skip("pending implementation — SC-17", () => {
            // AC2 — shows "Shared by <owner>" (the summary's owner display name), not the generic
            // "Shared" text the owner's own rows show.
        });
    });

    describe("Scenario: a share was revoked", () => {
        it.skip("pending implementation — SC-17", () => {
            // AC3 — no longer present in the grouped result once requestSharedWithMe() stops
            // returning it (backend already covered by FindSharedConversationsTests.cs).
        });
    });

    describe("Scenario: a shared entry's available entity actions", () => {
        it.skip("pending implementation — SC-17", () => {
            // AC4 — Pin/Archive/Rename/Move/Delete are not offered for a shared conversation; only
            // opening it is.
        });
    });
});
