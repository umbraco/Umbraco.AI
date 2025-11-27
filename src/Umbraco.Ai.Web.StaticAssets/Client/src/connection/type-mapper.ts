import type { ConnectionResponseModel, ConnectionItemResponseModel } from "../api/types.gen.js";
import { UaiConnectionConstants } from "./constants.js";
import type { UaiConnectionDetailModel, UaiConnectionItemModel } from "./types.js";

export const UaiConnectionTypeMapper = {
    toDetailModel(response: ConnectionResponseModel): UaiConnectionDetailModel {
        return {
            unique: response.id,
            entityType: UaiConnectionConstants.EntityType.Entity,
            alias: response.alias,
            name: response.name,
            providerId: response.providerId,
            settings: (response.settings as Record<string, unknown>) ?? null,
            isActive: response.isActive,
        };
    },

    toItemModel(response: ConnectionItemResponseModel): UaiConnectionItemModel {
        return {
            unique: response.id,
            entityType: UaiConnectionConstants.EntityType.Entity,
            name: response.name,
            providerId: response.providerId,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UaiConnectionDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            providerId: model.providerId,
            settings: model.settings,
            isActive: model.isActive,
        };
    },

    toUpdateRequest(model: UaiConnectionDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            settings: model.settings,
            isActive: model.isActive,
        };
    },
};
