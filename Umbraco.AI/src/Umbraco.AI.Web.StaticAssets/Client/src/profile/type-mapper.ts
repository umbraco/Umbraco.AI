import type {
    ProfileResponseModel,
    ProfileItemResponseModel,
    ChatProfileSettingsModel,
    EmbeddingProfileSettingsModel,
    SpeechToTextProfileSettingsModel,
} from "../api/types.gen.js";
import { UAI_PROFILE_ENTITY_TYPE } from "./constants.js";
import type {
    UaiProfileDetailModel,
    UaiProfileItemModel,
    UaiProfileSettings,
    UaiChatProfileSettings,
    UaiEmbeddingProfileSettings,
    UaiSpeechToTextProfileSettings,
} from "./types.js";
import { isChatSettings, isEmbeddingSettings, isSpeechToTextSettings } from "./types.js";

export const UaiProfileTypeMapper = {
    toDetailModel(response: ProfileResponseModel): UaiProfileDetailModel {
        return {
            unique: response.id,
            entityType: UAI_PROFILE_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            capability: response.capability,
            model: response.model ? { providerId: response.model.providerId, modelId: response.model.modelId } : null,
            connectionId: response.connectionId,
            settings: this.mapResponseSettings(response),
            tags: response.tags ?? [],
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version ?? 1,
        };
    },

    toItemModel(response: ProfileItemResponseModel): UaiProfileItemModel {
        return {
            unique: response.id,
            entityType: UAI_PROFILE_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            capability: response.capability,
            model: response.model ? { providerId: response.model.providerId, modelId: response.model.modelId } : null,
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiProfileDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            capability: model.capability,
            model: model.model!,
            connectionId: model.connectionId,
            settings: this.mapRequestSettings(model.settings),
            tags: model.tags,
        };
    },

    toUpdateRequest(model: UaiProfileDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            model: model.model!,
            connectionId: model.connectionId,
            settings: this.mapRequestSettings(model.settings),
            tags: model.tags,
        };
    },

    /**
     * Maps API response settings to internal model.
     * The API uses polymorphic JSON with $type discriminator.
     */
    mapResponseSettings(response: ProfileResponseModel): UaiProfileSettings | null {
        const settings = response.settings;
        if (!settings) return null;

        const type = settings.$type;
        if (type === "chat") {
            const chat = settings as ChatProfileSettingsModel;
            return {
                $type: "chat",
                temperature: chat.temperature ?? null,
                maxTokens: chat.maxTokens ?? null,
                systemPromptTemplate: chat.systemPromptTemplate ?? null,
                contextIds: chat.contextIds ?? [],
                guardrailIds: chat.guardrailIds ?? [],
            } as UaiChatProfileSettings;
        }

        if (type === "embedding") {
            const embedding = settings as EmbeddingProfileSettingsModel;
            return {
                $type: "embedding",
                dimensions: embedding.dimensions ?? null,
            } as UaiEmbeddingProfileSettings;
        }

        if (type === "speechToText") {
            const stt = settings as SpeechToTextProfileSettingsModel;
            return {
                $type: "speechToText",
                language: stt.language ?? null,
            } as UaiSpeechToTextProfileSettings;
        }

        return null;
    },

    /**
     * Maps internal model settings to API request format.
     */
    mapRequestSettings(
        settings: UaiProfileSettings | null,
    ): ChatProfileSettingsModel | EmbeddingProfileSettingsModel | SpeechToTextProfileSettingsModel | null {
        if (!settings) return null;

        if (isChatSettings(settings)) {
            return {
                $type: "chat",
                temperature: settings.temperature,
                maxTokens: settings.maxTokens,
                systemPromptTemplate: settings.systemPromptTemplate,
                contextIds: settings.contextIds,
                guardrailIds: settings.guardrailIds,
            } as ChatProfileSettingsModel;
        }

        if (isEmbeddingSettings(settings)) {
            return {
                $type: "embedding",
                dimensions: settings.dimensions,
            } as EmbeddingProfileSettingsModel;
        }

        if (isSpeechToTextSettings(settings)) {
            return {
                $type: "speechToText",
                language: settings.language,
            } as SpeechToTextProfileSettingsModel;
        }

        return null;
    },
};
