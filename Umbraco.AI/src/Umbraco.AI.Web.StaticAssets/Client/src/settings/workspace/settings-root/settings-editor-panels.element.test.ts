// MEM-01 — Add-on settings on the AI Settings screen (AC5, AC6)
import { describe, it } from "vitest";

// Real entry point: <uai-settings-editor> rendered with a fake UAI_SETTINGS_WORKSPACE_CONTEXT (same harness as
// settings-editor.element.test.ts), plus a `uaiSettingsPanel` manifest registered in umbExtensionsRegistry
// whose element writes Test.Flag through the context's additional-value setter.
// Pending specs: no imports of the not-yet-built modules, so this file loads before they exist. The builder
// adds the imports and fixture, fills each `it` with its single assertion (shown as a comment) and
// removes `.skip`.

describe("Feature: add-on settings panels on the AI Settings screen", () => {
    describe("Scenario: an add-on registered a settings panel", () => {
        // Arrange (beforeEach): register a `uaiSettingsPanel` manifest (test element) and render the editor

        // AC5
        it.skip("renders the panel's box after the Defaults and Transparency boxes", () => {
            // expect(boxHeadlines(el).slice(-1)[0]).toBe("Test panel"); // after "Defaults" and "Transparency"
        });
    });

    describe("Scenario: an admin changed a value in an add-on panel and clicks Save", () => {
        // Arrange (beforeEach): render the editor with the test panel, set Test.Flag=false from the panel, run the workspace's submit

        // AC6
        it.skip("sends the add-on value in the same save request as the built-in settings", () => {
            // expect(saveRequestBody).toMatchObject({ defaultChatProfileId: expect.anything(), additional: { "Test.Flag": false } });
        });
    });
});
