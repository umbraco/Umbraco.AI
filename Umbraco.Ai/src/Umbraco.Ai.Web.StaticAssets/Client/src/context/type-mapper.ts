import type { ContextResponseModel, ContextItemResponseModel, ContextResourceModel, CreateContextRequestModel, UpdateContextRequestModel } from "../api";
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
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toItemModel(response: ContextItemResponseModel): UaiContextItemModel {
        return {
            unique: response.id,
            entityType: UAI_CONTEXT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            resourceCount: response.resourceCount ?? 0,
            dateModified: response.dateModified,
        };
    },

    toResourceModel(resource: ContextResourceModel): UaiContextResourceModel {
        return {
            id: resource.id,
            resourceTypeId: resource.resourceTypeId,
            name: resource.name,
            description: resource.description ?? null,
            sortOrder: resource.sortOrder,
            // API returns object but generated types show string - cast through unknown until regenerated
            data: (resource.data as unknown as Record<string, unknown>) ?? null,
            injectionMode: resource.injectionMode as UaiContextResourceInjectionMode,
        };
    },

    toCreateRequest(model: UaiContextDetailModel): CreateContextRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            resources: model.resources.map(this.toResourceRequest),
        };
    },

    toUpdateRequest(model: UaiContextDetailModel): UpdateContextRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            resources: model.resources.map(this.toResourceRequest),
        };
    },

    toResourceRequest(resource: UaiContextResourceModel): ContextResourceModel {
        return {
            id: resource.id,
            resourceTypeId: resource.resourceTypeId,
            name: resource.name,
            description: resource.description,
            sortOrder: resource.sortOrder,
            // API expects object but generated types show string - cast through unknown until regenerated
            data: resource.data as unknown as string,
            injectionMode: resource.injectionMode,
        };
    },
};
