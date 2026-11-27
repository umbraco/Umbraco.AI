// MEM-24 — An admin erases a user's AI data (AC4, AC5, AC6, AC11, AC12)
import { describe, it } from "vitest";

// Real entry point: the "Erase AI memory and personal context" user entity action, registered by the
// package manifests and run against a fake user entity (Sophie Larsen), with a fake erase repository
// (the API client behind it), the danger confirm modal rendered from the real modal manager, and the
// `uaiUserDataEraseItem` manifests registered in umbExtensionsRegistry (core's personal contexts item,
// plus the Agent package's memories item for AC5).
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The
// builder adds the imports and fixture, fills each `it` with its single assertion (shown as a
// comment) and removes `.skip`.

describe("Feature: an admin erases a user's AI data", () => {
    describe("Scenario: an admin with Users section access opens a user's actions menu", () => {
        // Arrange (beforeEach): register the entity action for the user entity type; admin has Users access; list the user's actions

        // AC4
        it.skip('lists "Erase AI memory and personal context"', () => {
            // expect(actionLabels(el)).toContain("Erase AI memory and personal context");
        });
    });

    describe("Scenario: the Agent package is installed and the admin clicks the action", () => {
        // Arrange (beforeEach): register uaiUserDataEraseItem manifests for personal contexts and resources, and for "About you" and agent memories; run the action

        // AC5
        it.skip("lists personal contexts and resources, and About you and agent memories in the danger dialog", () => {
            // expect(confirmItemLabels(dialog)).toEqual([personalContextsLabel, aboutYouLabel]); // labels from the manifests
        });
    });

    describe("Scenario: the dialog is open and the admin clicks Erase", () => {
        // Arrange (beforeEach): the fake erase repository succeeds; open the dialog for Sophie Larsen and click Erase

        // AC6
        it.skip('shows the toast "AI data erased for Sophie Larsen"', () => {
            // expect(peekedNotifications).toContainEqual(expect.objectContaining({ data: expect.objectContaining({ message: "AI data erased for Sophie Larsen" }) }));
        });

        // AC6
        it.skip("closes the dialog", () => {
            // expect(dialogIsOpen()).toBe(false);
        });
    });

    describe("Scenario: only core is installed and the admin clicks the action", () => {
        // Arrange (beforeEach): register only core's uaiUserDataEraseItem (personal contexts and resources); run the action

        // AC12
        it.skip("lists personal contexts and resources only", () => {
            // expect(confirmItemLabels(dialog)).toEqual([personalContextsLabel]);
        });
    });

    describe("Sad path: the erase request fails", () => {
        // Arrange (beforeEach): the fake erase repository returns an error; open the dialog and click Erase

        // AC11
        it.skip("shows an error toast", () => {
            // expect(peekedNotifications).toContainEqual(expect.objectContaining({ color: "danger" }));
        });

        // AC11
        it.skip("keeps the dialog open", () => {
            // expect(dialogIsOpen()).toBe(true);
        });
    });
});
