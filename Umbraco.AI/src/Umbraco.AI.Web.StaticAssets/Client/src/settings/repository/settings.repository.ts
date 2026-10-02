import { SettingsService } from "../../api/sdk.gen.js";
import type { UaiSettingsModel } from "../types.js";
import type { UaiDisclosureNoticeMode } from "../../disclosure/types.js";

// Anything unexpected reads as Always, matching the server: a bad value must never hide the notice.
function toDisclosureNoticeMode(value: string | undefined): UaiDisclosureNoticeMode {
    return value === "Dismissible" || value === "Off" ? value : "Always";
}

/**
 * Repository for AI settings data access.
 */
export class UaiSettingsRepository {
    async get(): Promise<UaiSettingsModel> {
        const { data } = await SettingsService.getSettings();
        return {
            defaultChatProfileId: data?.defaultChatProfileId ?? null,
            defaultEmbeddingProfileId: data?.defaultEmbeddingProfileId ?? null,
            defaultSpeechToTextProfileId: data?.defaultSpeechToTextProfileId ?? null,
            defaultImageGenerationProfileId: data?.defaultImageGenerationProfileId ?? null,
            defaultDecisionProfileId: data?.defaultDecisionProfileId ?? null,
            classifierChatProfileId: data?.classifierChatProfileId ?? null,
            disclosureNoticeMode: toDisclosureNoticeMode(data?.disclosureNoticeMode),
        };
    }

    async save(model: UaiSettingsModel): Promise<UaiSettingsModel> {
        const { data } = await SettingsService.updateSettings({
            body: {
                defaultChatProfileId: model.defaultChatProfileId ?? undefined,
                defaultEmbeddingProfileId: model.defaultEmbeddingProfileId ?? undefined,
                defaultSpeechToTextProfileId: model.defaultSpeechToTextProfileId ?? undefined,
                defaultImageGenerationProfileId: model.defaultImageGenerationProfileId ?? undefined,
                defaultDecisionProfileId: model.defaultDecisionProfileId ?? undefined,
                classifierChatProfileId: model.classifierChatProfileId ?? undefined,
                disclosureNoticeMode: model.disclosureNoticeMode,
            },
        });
        return {
            defaultChatProfileId: data?.defaultChatProfileId ?? null,
            defaultEmbeddingProfileId: data?.defaultEmbeddingProfileId ?? null,
            defaultSpeechToTextProfileId: data?.defaultSpeechToTextProfileId ?? null,
            defaultImageGenerationProfileId: data?.defaultImageGenerationProfileId ?? null,
            defaultDecisionProfileId: data?.defaultDecisionProfileId ?? null,
            classifierChatProfileId: data?.classifierChatProfileId ?? null,
            disclosureNoticeMode: toDisclosureNoticeMode(data?.disclosureNoticeMode),
        };
    }
}

export const settingsRepository = new UaiSettingsRepository();
