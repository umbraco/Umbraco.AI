import type {
    ProfileResponseModel,
    ProfileItemResponseModel,
    ChatProfileSettingsModel,
    EmbeddingProfileSettingsModel,
} from "../api/types.gen.js";
import { UAI_PROFILE_ENTITY_TYPE } from "./constants.js";
import type {
    UaiProfileDetailModel,
    UaiProfileItemModel,
    UaiProfileSettings,
    UaiChatProfileSettings,
} from "./types.js";
import { isChatSettings } from "./types.js";

export const UaiProfileTypeMapper = {
    toDetailModel(response: ProfileResponseModel): UaiProfileDetailModel {
        // Note: Cast to access version until API client is regenerated
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const version = (response as any).version as number | undefined;
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
            version: version ?? 1,
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
     * Note: Uses 'any' cast because generated types may not include 'settings' until regenerated.
     */
    mapResponseSettings(response: ProfileResponseModel): UaiProfileSettings | null {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const settings = (response as any).settings as Record<string, unknown> | undefined;
        if (!settings) return null;

        // The API returns settings with $type discriminator
        const type = settings.$type as string;
        if (type === "chat") {
            return {
                $type: "chat",
                temperature: (settings.temperature as number) ?? null,
                maxTokens: (settings.maxTokens as number) ?? null,
                systemPromptTemplate: (settings.systemPromptTemplate as string) ?? null,
                contextIds: (settings.contextIds as string[] | undefined) ?? [],
            } as UaiChatProfileSettings;
        }

        if (type === "embedding") {
            return { $type: "embedding" };
        }

        return null;
    },

    /**
     * Maps internal model settings to API request format.
     */
    mapRequestSettings(
        settings: UaiProfileSettings | null,
    ): ChatProfileSettingsModel | EmbeddingProfileSettingsModel | null {
        if (!settings) return null;

        if (isChatSettings(settings)) {
            return {
                $type: "chat",
                temperature: settings.temperature,
                maxTokens: settings.maxTokens,
                systemPromptTemplate: settings.systemPromptTemplate,
                contextIds: settings.contextIds,
            } as ChatProfileSettingsModel;
        }

        // Embedding settings
        return {
            $type: "embedding",
        } as EmbeddingProfileSettingsModel;
    },
};
