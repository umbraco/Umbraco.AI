// DR-6 — Hide disabled experimental capabilities (AC4); DR-3 — Set a default Decision profile (AC3)
import { beforeEach, describe, expect, it, vi } from "vitest";

// Stub at the real boundary: the generated OpenAPI SDK.
const updateSettings = vi.fn();
vi.mock("../../api/sdk.gen.js", () => ({
    SettingsService: {
        getSettings: vi.fn(),
        updateSettings: (...args: unknown[]) => updateSettings(...args),
    },
}));

import { UaiSettingsRepository } from "./settings.repository.js";
import type { UaiSettingsModel } from "../types.js";

const stored: UaiSettingsModel = {
    defaultChatProfileId: null,
    defaultEmbeddingProfileId: null,
    defaultSpeechToTextProfileId: null,
    defaultImageGenerationProfileId: "22222222-2222-2222-2222-222222222222",
    defaultDecisionProfileId: "33333333-3333-3333-3333-333333333333",
    classifierChatProfileId: null,
};

describe("Feature: saving settings", () => {
    describe("Scenario: experimental pickers are hidden but values are stored", () => {
        beforeEach(async () => {
            updateSettings.mockReset();
            updateSettings.mockResolvedValue({ data: {} });
            // The editor hides pickers but saves the whole model from the workspace context.
            await new UaiSettingsRepository().save(stored);
        });

        // Pending T15
        it.skip("keeps the stored image generation default", () => {
            expect(updateSettings.mock.calls[0][0].body.defaultImageGenerationProfileId).toBe(
                "22222222-2222-2222-2222-222222222222",
            );
        });

        // Pending T15
        it.skip("sends the default Decision profile", () => {
            expect(updateSettings.mock.calls[0][0].body.defaultDecisionProfileId).toBe(
                "33333333-3333-3333-3333-333333333333",
            );
        });
    });
});
