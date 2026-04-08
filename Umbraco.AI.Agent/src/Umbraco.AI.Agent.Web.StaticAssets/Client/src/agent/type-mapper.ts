import type {
    AgentResponseModel,
    AgentItemResponseModel,
    CreateAgentRequestModel,
    UpdateAgentRequestModel,
    StandardAgentConfigModel,
    OrchestratedAgentConfigModel,
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

function mapConfigFromResponse(agentType: string, config: StandardAgentConfigModel | OrchestratedAgentConfigModel | null | undefined): UaiAgentConfig {
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
        outputSchema?: Record<string, unknown>;
        userGroupPermissions?: Record<string, unknown>;
    } | null;
    return {
        $type: "standard",
        contextIds: standard?.contextIds ?? [],
        instructions: standard?.instructions ?? null,
        allowedToolIds: standard?.allowedToolIds ?? [],
        allowedToolScopeIds: standard?.allowedToolScopeIds ?? [],
        outputSchema: (standard?.outputSchema as Record<string, unknown>) ?? null,
        userGroupPermissions: (standard?.userGroupPermissions as UaiStandardAgentConfig["userGroupPermissions"]) ?? {},
    } satisfies UaiStandardAgentConfig;
}

function mapConfigToRequest(config: UaiAgentConfig): StandardAgentConfigModel | OrchestratedAgentConfigModel {
    if (config.$type === "orchestrated") {
        return {
            $type: "orchestrated",
            workflowId: config.workflowId,
            settings: config.settings,
        } as OrchestratedAgentConfigModel;
    }

    return {
        $type: "standard",
        contextIds: config.contextIds,
        instructions: config.instructions,
        allowedToolIds: config.allowedToolIds,
        allowedToolScopeIds: config.allowedToolScopeIds,
        outputSchema: config.outputSchema,
        userGroupPermissions: config.userGroupPermissions,
    } as StandardAgentConfigModel;
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
            guardrailIds: response.guardrailIds ?? [],
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
            guardrailIds: (response as any).guardrailIds ?? [],
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
            guardrailIds: model.guardrailIds,
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
            guardrailIds: model.guardrailIds,
            isActive: model.isActive,
        };
    },
};
