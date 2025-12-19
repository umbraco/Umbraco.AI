import type { AgentResponseModel, AgentItemResponseModel } from "../api/types.gen.js";
import { UAI_AGENT_ENTITY_TYPE } from "./constants.js";
import type { UAiAgentDetailModel, UAiAgentItemModel } from "./types.js";


export const UAiAgentTypeMapper = {
    toDetailModel(response: AgentResponseModel): UAiAgentDetailModel {
        return {
            unique: response.id,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            content: response.content,
            profileId: response.profileId ?? null,
            tags: response.tags ?? [],
            scope: null,
            isActive: response.isActive,
        };
    },

    toItemModel(response: AgentItemResponseModel): UAiAgentItemModel {
        return {
            unique: response.id,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UAiAgentDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
            scope: null,
        };
    },

    toUpdateRequest(model: UAiAgentDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
            scope: null,
            isActive: model.isActive,
        };
    },
};
