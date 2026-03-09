import type {
    AgentResponseModel,
    AgentItemResponseModel,
    CreateAgentRequestModel,
    UpdateAgentRequestModel,
    AgentConfigModel,
} from "../api/types.gen.js";
import { UAI_AGENT_ENTITY_TYPE } from "./constants.js";
import type {
    UaiAgentDetailModel,
    UaiAgentItemModel,
    UaiAgentConfig,
    UaiAgentType,
    UaiStandardAgentConfig,
    UaiOrchestratedAgentConfig,
} from "./types.js";

function mapConfigFromResponse(agentType: string, config: AgentConfigModel | null | undefined): UaiAgentConfig {
    if (agentType === "orchestrated") {
        const orchestrated = config as { $type?: string; workflowId?: string; settings?: unknown } | null;
        return {
            $type: "orchestrated",
            workflowId: orchestrated?.workflowId ?? null,
            settings: orchestrated?.settings ?? null,
        } satisfies UaiOrchestratedAgentConfig;
    }

    // Default: standard
    const standard = config as {
        $type?: string;
        contextIds?: string[];
        instructions?: string;
        allowedToolIds?: string[];
        allowedToolScopeIds?: string[];
        userGroupPermissions?: Record<string, unknown>;
    } | null;
    return {
        $type: "standard",
        contextIds: standard?.contextIds ?? [],
        instructions: standard?.instructions ?? null,
        allowedToolIds: standard?.allowedToolIds ?? [],
        allowedToolScopeIds: standard?.allowedToolScopeIds ?? [],
        userGroupPermissions: (standard?.userGroupPermissions as UaiStandardAgentConfig["userGroupPermissions"]) ?? {},
    } satisfies UaiStandardAgentConfig;
}

function mapConfigToRequest(config: UaiAgentConfig): AgentConfigModel {
    if (config.$type === "orchestrated") {
        return {
            $type: "orchestrated",
            workflowId: config.workflowId,
            settings: config.settings,
        } as AgentConfigModel;
    }

    return {
        $type: "standard",
        contextIds: config.contextIds,
        instructions: config.instructions,
        allowedToolIds: config.allowedToolIds,
        allowedToolScopeIds: config.allowedToolScopeIds,
        userGroupPermissions: config.userGroupPermissions,
    } as AgentConfigModel;
}

export const UaiAgentTypeMapper = {
    toDetailModel(response: AgentResponseModel): UaiAgentDetailModel {
        const agentType = (response.agentType ?? "standard") as UaiAgentType;
        return {
            unique: response.id,
            entityType: UAI_AGENT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            agentType,
            profileId: response.profileId ?? null,
            surfaceIds: response.surfaceIds ?? [],
            scope: (response as any).scope ?? null,
            config: mapConfigFromResponse(agentType, response.config),
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
            agentType: (response.agentType ?? "standard") as UaiAgentType,
            profileId: response.profileId ?? null,
            surfaceIds: response.surfaceIds ?? [],
            scope: (response as any).scope ?? null,
            isActive: response.isActive,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiAgentDetailModel): CreateAgentRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            agentType: model.agentType,
            description: model.description,
            profileId: model.profileId,
            surfaceIds: model.surfaceIds,
            scope: model.scope,
            config: mapConfigToRequest(model.config),
        };
    },

    toUpdateRequest(model: UaiAgentDetailModel): UpdateAgentRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            surfaceIds: model.surfaceIds,
            scope: model.scope,
            config: mapConfigToRequest(model.config),
            isActive: model.isActive,
        };
    },
};
