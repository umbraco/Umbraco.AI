// STAGED SPEC — see share-conversation.spec.ts's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/, as part of SC-10).
// Sibling to types.test.ts's existing toConversationDetailModel() specs — toSharedConversationDetailModel()
// belongs in that same file/module per SPEC.md.

import { describe, it, expect } from "vitest";

// STORIES.md US4 — Read a conversation shared with me.
// Real entry point exercised: toSharedConversationDetailModel() (the mapping SC-10 adds) and the
// workspace-context's derived `readonly` field / isReadonly$ observable that both the chat view and
// context panel consume.

describe("Feature: read a shared conversation", () => {
    describe("Scenario: Snapshot share", () => {
        it.skip("pending implementation — SC-10", () => {
            // AC1 — toSharedConversationDetailModel() maps the shared-view response's message list
            // as-is (cutoff enforcement is a backend concern, per ReadSharedConversationTests.cs);
            // this spec covers the mapping shape, not the cutoff logic itself.
        });
    });

    describe("Scenario: Live share, reloaded after the owner sends another message", () => {
        it.skip("pending implementation — SC-10", () => {
            // AC2 — a second requestSharedView() call reflects the new message; no client-side
            // caching hides it.
        });
    });

    describe("Scenario: viewing the chat", () => {
        it.skip("pending implementation — SC-10", () => {
            // AC3 — isReadonly$ is true for a mapped shared conversation (isShared: true), and the
            // readonly-notice text names the owner rather than reusing the archived-conversation
            // copy.
        });
    });

    describe("Scenario: viewing the context panel", () => {
        it.skip("pending implementation — SC-10", () => {
            // AC4 — the panel's resource/context pickers render conversation.readonly as true, so
            // "add a resource/context" is disabled, while still showing the existing (unfiltered)
            // list.
        });
    });

    describe("Scenario: the mapped detail model for a plain (non-shared, non-archived) conversation", () => {
        it.skip("pending implementation — SC-10", () => {
            // Regression guard: toConversationDetailModel() (the existing owner-side mapper) still
            // produces isShared: false / readonly: false — this feature must not change owner-side
            // behavior.
        });
    });
});
