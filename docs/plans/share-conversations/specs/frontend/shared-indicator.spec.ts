// STAGED SPEC — not yet in a real test project. Move to
// Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-13,
// alongside tree-item.element.ts's own tests (if any exist by then) or as a new file next to it.

import { describe, it, expect } from "vitest";

// STORIES.md US2 AC5-7 — the owner's own conversation rows show a real Umbraco entity-sign badge
// when the conversation has active shares (ARCHITECTURE.md decision 6), not an icon swap.
// Real entry point exercised: the entitySign manifest's `forEntityFlags` match against
// `hasActiveShares`-derived flags, and uai-copilot-workspace-conversation-tree-item wrapping its
// existing icon in <umb-entity-sign-bundle>.

describe("Feature: owner-side shared indicator", () => {
    describe("Scenario: conversation has an active share", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC5 — the tree item passes entityFlags: [{ alias: 'Uai.Conversation.IsShared' }] when
            // hasActiveShares is true, and no flags when false.
        });
    });

    describe("Scenario: conversation is both pinned and shared", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC6 — the main icon stays the pushpin (unchanged pin-vs-chat logic); the entity-sign
            // badge overlays on top of it regardless of which icon is showing. This is the one
            // behavior the superseded icon-swap design (see ARCHITECTURE.md decision 6) could not do.
        });
    });

    describe("Scenario: hovering the badge", () => {
        it.skip("pending implementation — SC-13", () => {
            // AC7 — shows the entitySign manifest's label ("Shared") as a tooltip; does not navigate
            // or open anything.
        });
    });
});
