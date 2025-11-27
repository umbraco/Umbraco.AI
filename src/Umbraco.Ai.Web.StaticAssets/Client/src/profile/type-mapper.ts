import type { ProfileResponseModel, ProfileItemResponseModel } from "../api/types.gen.js";
import { UAI_PROFILE_ENTITY_TYPE } from "./constants.js";
import type { UaiProfileDetailModel, UaiProfileItemModel } from "./types.js";

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
            temperature: response.temperature ?? null,
            maxTokens: response.maxTokens ?? null,
            systemPromptTemplate: response.systemPromptTemplate ?? null,
            tags: response.tags ?? [],
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
        };
    },

    toCreateRequest(model: UaiProfileDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            capability: model.capability,
            model: model.model!,
            connectionId: model.connectionId,
            temperature: model.temperature,
            maxTokens: model.maxTokens,
            systemPromptTemplate: model.systemPromptTemplate,
            tags: model.tags,
        };
    },

    toUpdateRequest(model: UaiProfileDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            model: model.model!,
            connectionId: model.connectionId,
            temperature: model.temperature,
            maxTokens: model.maxTokens,
            systemPromptTemplate: model.systemPromptTemplate,
            tags: model.tags,
        };
    },
};
