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
            profileId: response.profileId,
            instructions: response.instructions ?? null,
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
            profileId: response.profileId,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UAiAgentDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            instructions: model.instructions,
        };
    },

    toUpdateRequest(model: UAiAgentDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            instructions: model.instructions,
            isActive: model.isActive,
        };
    },
};
