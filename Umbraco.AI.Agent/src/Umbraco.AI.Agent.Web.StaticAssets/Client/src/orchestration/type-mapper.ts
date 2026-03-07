import type {
    OrchestrationResponseModel,
    OrchestrationItemResponseModel,
    CreateOrchestrationRequestModel,
    UpdateOrchestrationRequestModel,
} from "../api/types.gen.js";
import { UAI_ORCHESTRATION_ENTITY_TYPE } from "./constants.js";
import type { UaiOrchestrationDetailModel, UaiOrchestrationItemModel } from "./types.js";

export const UaiOrchestrationTypeMapper = {
    toDetailModel(response: OrchestrationResponseModel): UaiOrchestrationDetailModel {
        return {
            unique: response.id,
            entityType: UAI_ORCHESTRATION_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            profileId: response.profileId ?? null,
            surfaceIds: response.surfaceIds ?? [],
            scope: (response as any).scope ?? null,
            graph: (response as any).graph ?? { nodes: [], edges: [] },
            isActive: response.isActive,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version,
        };
    },

    toItemModel(response: OrchestrationItemResponseModel): UaiOrchestrationItemModel {
        return {
            unique: response.id,
            entityType: UAI_ORCHESTRATION_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            profileId: response.profileId ?? null,
            surfaceIds: response.surfaceIds ?? [],
            scope: (response as any).scope ?? null,
            isActive: response.isActive,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiOrchestrationDetailModel): CreateOrchestrationRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            surfaceIds: model.surfaceIds,
            scope: model.scope,
            graph: model.graph,
        } as CreateOrchestrationRequestModel;
    },

    toUpdateRequest(model: UaiOrchestrationDetailModel): UpdateOrchestrationRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            profileId: model.profileId,
            surfaceIds: model.surfaceIds,
            scope: model.scope,
            graph: model.graph,
            isActive: model.isActive,
        } as UpdateOrchestrationRequestModel;
    },
};
