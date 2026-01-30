import { SettingsService } from "../../api/sdk.gen.js";
import type { UaiSettingsModel } from "../types.js";

/**
 * Repository for AI settings data access.
 */
export class UaiSettingsRepository {
    async get(): Promise<UaiSettingsModel> {
        const { data } = await SettingsService.getSettings();
        return {
            defaultChatProfileId: data?.defaultChatProfileId ?? null,
            defaultEmbeddingProfileId: data?.defaultEmbeddingProfileId ?? null,
        };
    }

    async save(model: UaiSettingsModel): Promise<UaiSettingsModel> {
        const { data } = await SettingsService.updateSettings({
            body: {
                defaultChatProfileId: model.defaultChatProfileId ?? undefined,
                defaultEmbeddingProfileId: model.defaultEmbeddingProfileId ?? undefined,
            },
        });
        return {
            defaultChatProfileId: data?.defaultChatProfileId ?? null,
            defaultEmbeddingProfileId: data?.defaultEmbeddingProfileId ?? null,
        };
    }
}

export const settingsRepository = new UaiSettingsRepository();
