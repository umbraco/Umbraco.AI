import type { PromptResponseModel, PromptItemResponseModel } from "../api/types.gen.js";
import { UAI_PROMPT_ENTITY_TYPE } from "./constants.js";
import type { UaiPromptDetailModel, UaiPromptItemModel } from "./types.js";

export const UaiPromptTypeMapper = {
    toDetailModel(response: PromptResponseModel): UaiPromptDetailModel {
        return {
            unique: response.id,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            content: response.content,
            profileId: response.profileId ?? null,
            tags: response.tags ?? [],
            isActive: response.isActive,
        };
    },

    toItemModel(response: PromptItemResponseModel): UaiPromptItemModel {
        return {
            unique: response.id,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
        };
    },

    toUpdateRequest(model: UaiPromptDetailModel) {
        return {
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
            isActive: model.isActive,
        };
    },
};
