// MEM-18 — Keep my own contexts and resources (AC2, AC7, AC8, AC11-AC14)
import { describe, it } from "vitest";

// Real entry point: the "Your AI" user profile app element, rendered with a fake personal context
// repository (the API client behind it) and the real uai-context-picker and uai-resource-list.
// AC8 and AC13 register (or don't) a `uaiUserProfileProperty` manifest in umbExtensionsRegistry,
// standing in for the Agent package's memory switch.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The
// builder adds the imports and fixture, fills each `it` with its single assertion (shown as a
// comment) and removes `.skip`.

describe("Feature: keep my own contexts and resources", () => {
    describe("Scenario: a user adds an Always Text resource", () => {
        // Arrange (beforeEach): render with an empty set; add Text resource "How I write product pages", Always

        // AC2
        it.skip("shows it under Resources tagged Always", () => {
            // expect(resourceRow(el, "How I write product pages").tag).toBe("Always");
        });
    });

    describe("Scenario: a user who never saved a personal context", () => {
        // Arrange (beforeEach): the fake repository returns { contextIds: [], resources: [] }

        // AC7
        it.skip("shows only the Add buttons in the Contexts and Resources rows", () => {
            // expect(rowItems(el, "Contexts").length + rowItems(el, "Resources").length).toBe(0);
        });
    });

    describe("Scenario: the Agent package adds the memory switch", () => {
        // Arrange (beforeEach): register a uaiUserProfileProperty manifest labelled "Let agents remember things about me"

        // AC8
        it.skip("shows it as the last row of the Your AI box", () => {
            // expect(propertyLabels(el).at(-1)).toBe("Let agents remember things about me");
        });
    });

    describe("Scenario: no uaiUserProfileProperty is registered (no Agent package)", () => {
        // Arrange (beforeEach): render with nothing registered for uaiUserProfileProperty

        // AC13
        it.skip("shows Contexts and Resources and no other rows", () => {
            // expect(propertyLabels(el)).toEqual(["Contexts", "Resources"]);
        });
    });

    describe("Scenario: the site's Personal Context switch is off", () => {
        // Arrange (beforeEach): the fake repository returns personalContextEnabled: false

        // AC14
        it.skip("hides the Contexts and Resources rows behind the admin note", () => {
            // expect(noteText(el)).toBe("Your admin has turned off personal context"); // and no Contexts/Resources rows
        });
    });

    describe("Sad path: the server fails the save", () => {
        // Arrange (beforeEach): saved set has one resource; the fake repository's save returns an error; add a second resource

        // AC11
        it.skip("shows an error toast", () => {
            // expect(peekedNotifications).toContainEqual(expect.objectContaining({ color: "danger" }));
        });

        // AC12
        it.skip("puts the Resources list back as it was", () => {
            // expect(rowItems(el, "Resources").map((r) => r.name)).toEqual(["How I write product pages"]);
        });
    });
});
