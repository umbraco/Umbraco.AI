import type { ContextResponseModel, ContextItemResponseModel, ContextResourceResponseModel } from "../api/types.gen.js";
import { UAI_CONTEXT_ENTITY_TYPE } from "./constants.js";
import type { UaiContextDetailModel, UaiContextItemModel, UaiContextResourceModel, UaiContextResourceInjectionMode } from "./types.js";

export const UaiContextTypeMapper = {
    toDetailModel(response: ContextResponseModel): UaiContextDetailModel {
        return {
            unique: response.id,
            entityType: UAI_CONTEXT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            resources: (response.resources ?? []).map(this.toResourceModel),
        };
    },

    toItemModel(response: ContextItemResponseModel): UaiContextItemModel {
        return {
            unique: response.id,
            entityType: UAI_CONTEXT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            resourceCount: response.resourceCount ?? 0,
        };
    },

    toResourceModel(resource: ContextResourceResponseModel): UaiContextResourceModel {
        return {
            id: resource.id,
            resourceTypeId: resource.resourceTypeId,
            name: resource.name,
            description: resource.description ?? null,
            sortOrder: resource.sortOrder,
            data: resource.data,
            injectionMode: resource.injectionMode as UaiContextResourceInjectionMode,
        };
    },

    toCreateRequest(model: UaiContextDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            resources: model.resources.map(this.toResourceRequest),
        };
    },

    toUpdateRequest(model: UaiContextDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            resources: model.resources.map(this.toResourceRequest),
        };
    },

    toResourceRequest(resource: UaiContextResourceModel) {
        return {
            id: resource.id,
            resourceTypeId: resource.resourceTypeId,
            name: resource.name,
            description: resource.description,
            sortOrder: resource.sortOrder,
            data: resource.data,
            injectionMode: resource.injectionMode,
        };
    },
};
