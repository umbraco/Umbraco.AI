// EX-4 — Set a default Extraction profile (AC6)
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
    defaultImageGenerationProfileId: null,
    defaultDecisionProfileId: null,
    defaultExtractionProfileId: "44444444-4444-4444-4444-444444444444",
    classifierChatProfileId: null,
    disclosureNoticeMode: "Always",
};

describe("Feature: saving settings with a default Extraction profile", () => {
    describe("Scenario: the Extraction picker is hidden but a value is stored", () => {
        beforeEach(async () => {
            updateSettings.mockReset();
            updateSettings.mockResolvedValue({ data: {} });
            // PUT settings is full-replace: the editor hides the picker but saves the whole model.
            await new UaiSettingsRepository().save(stored);
        });

        it("sends the stored default Extraction profile unchanged", () => {
            expect(updateSettings.mock.calls[0][0].body.defaultExtractionProfileId).toBe(
                "44444444-4444-4444-4444-444444444444",
            );
        });
    });
});
