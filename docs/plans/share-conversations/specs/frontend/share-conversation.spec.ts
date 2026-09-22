// STAGED SPEC — not yet in a real test project (Umbraco.AI.Agent.Copilot.Workspace doesn't exist on
// this branch yet, see PLAN.md's blocked note). Move to
// Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/src/conversation/ as part of SC-11.
// Uses this codebase's actual convention: flat Vitest describe/it over plain functions/classes
// (see grouping.test.ts, types.test.ts), not @open-wc fixture-rendered custom elements — no test in
// this project renders full shadow DOM, so this matches rather than invents a new pattern.

import { describe, it, expect } from "vitest";

// STORIES.md US1 — Share a conversation with named colleagues.
// Real entry point exercised: UaiConversationRepository.share() (SPEC.md's
// `share(conversation, recipientUserKeys, scopeMode)` → POST .../shares), and the Share modal's
// disclosure-notice visibility logic.

describe("Feature: share a conversation", () => {
    describe("Scenario: single recipient, Snapshot scope", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC1 — submitting the modal with one recipient and Snapshot calls
            // repository.share(conversation, [recipientKey], "snapshot").
        });
    });

    describe("Scenario: multiple recipients in one submit", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC2 — one repository.share() call carries every picked recipient, not one call each.
        });
    });

    describe("Scenario: Live scope", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC3
        });
    });

    describe("Scenario: re-sharing to an already-shared recipient", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC4 — the modal doesn't need special-case UI for this; the backend upserts (SC-04).
            // This spec covers that the modal doesn't block or duplicate the submission client-side.
        });
    });

    describe("Scenario: conversation has attached resources/contexts", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC5 — the disclosure notice renders when contextIds/resources is non-empty.
        });
    });

    describe("Scenario: conversation has no attachments", () => {
        it.skip("pending implementation — SC-11", () => {
            // AC5, negative case — no disclosure notice when there's nothing to disclose.
        });
    });
});
