import type {
    AgentResponseModel,
    AgentItemResponseModel,
    CreateAgentRequestModel,
    UpdateAgentRequestModel,
} from "../api/types.gen.js";
import { UAI_AGENT_ENTITY_TYPE } from "./constants.js";
import type { UaiAgentDetailModel, UaiAgentItemModel } from "./types.js";

export const UaiAgentTypeMapper = {
    toDetailModel(response: AgentResponseModel): UaiAgentDetailModel {
        return {
            unique: response.id,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            profileId: response.profileId ?? null,
            contextIds: response.contextIds ?? [],
            scopeIds: response.scopeIds ?? [],
            contextScope: (response as any).contextScope ?? null,
            allowedToolIds: response.allowedToolIds ?? [],
            allowedToolScopeIds: response.allowedToolScopeIds ?? [],
            userGroupPermissions: (response as any).userGroupPermissions ?? {},
            instructions: response.instructions ?? null,
            isActive: response.isActive,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version,
        };
    },

    toItemModel(response: AgentItemResponseModel): UaiAgentItemModel {
        return {
            unique: response.id,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            profileId: response.profileId ?? null,
            contextIds: response.contextIds ?? [],
            scopeIds: response.scopeIds ?? [],
            contextScope: (response as any).contextScope ?? null,
            allowedToolIds: response.allowedToolIds ?? [],
            allowedToolScopeIds: response.allowedToolScopeIds ?? [],
            isActive: response.isActive,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiAgentDetailModel): CreateAgentRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            contextIds: model.contextIds,
            scopeIds: model.scopeIds,
            contextScope: model.contextScope,
            allowedToolIds: model.allowedToolIds,
            allowedToolScopeIds: model.allowedToolScopeIds,
            userGroupPermissions: model.userGroupPermissions,
            instructions: model.instructions,
        } as CreateAgentRequestModel;
    },

    toUpdateRequest(model: UaiAgentDetailModel): UpdateAgentRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            contextIds: model.contextIds,
            scopeIds: model.scopeIds,
            contextScope: model.contextScope,
            allowedToolIds: model.allowedToolIds,
            allowedToolScopeIds: model.allowedToolScopeIds,
            userGroupPermissions: model.userGroupPermissions,
            instructions: model.instructions,
            isActive: model.isActive,
        } as UpdateAgentRequestModel;
    },
};
