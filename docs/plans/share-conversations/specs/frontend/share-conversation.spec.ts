// STAGED SPEC — not yet in a real test project (Umbraco.AI.Agent.Copilot.Workspace doesn't exist on
// this branch yet). Move to
// Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/ as part of SC-15.
// Uses this codebase's actual convention: flat Vitest describe/it over plain functions/classes
// (see grouping.test.ts, types.test.ts), not @open-wc fixture-rendered custom elements — no test in
// this project renders full shadow DOM, so this matches rather than invents a new pattern.

import { describe, it, expect } from "vitest";

// STORIES.md US1 — Share a conversation with a colleague.
// Real entry point exercised: UaiConversationRepository.share() (SPEC.md's
// `share(conversation, recipientUserKey, scopeMode)` → POST .../shares — one recipient per call,
// not an array, per ARCHITECTURE.md decision 7), and the share-picker modal's disclosure-notice
// visibility and submit-disabled logic.

describe("Feature: share a conversation", () => {
    describe("Scenario: one recipient, Snapshot mode", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC1 — picking one colleague and "Snapshot", then submitting, calls
            // repository.share(conversation, recipientKey, "snapshot") and closes the modal.
        });
    });

    describe("Scenario: Live mode", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC3
        });
    });

    describe("Scenario: re-sharing to an already-shared recipient", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC4 — the modal doesn't need special-case UI for this; the backend upserts (SC-04).
            // This spec covers that the modal doesn't block or duplicate the submission client-side.
        });
    });

    describe("Scenario: conversation has attached resources/contexts", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC5 — the disclosure notice renders when contextIds/resources is non-empty.
        });
    });

    describe("Scenario: conversation has no attachments", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC6, negative case — no disclosure notice when there's nothing to disclose.
        });
    });

    describe("Scenario: no recipient picked yet", () => {
        it.skip("pending implementation — SC-15", () => {
            // AC10 — the "Share" submit button is disabled until the one-item recipient field has a
            // pick.
        });
    });
});
