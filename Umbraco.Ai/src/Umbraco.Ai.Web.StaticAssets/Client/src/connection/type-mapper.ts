import type { ConnectionResponseModel, ConnectionItemResponseModel, ModelDescriptorResponseModel } from "../api/types.gen.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "./constants.js";
import type { UaiConnectionDetailModel, UaiConnectionItemModel, UaiModelDescriptorModel } from "./types.js";

export const UaiConnectionTypeMapper = {
    toDetailModel(response: ConnectionResponseModel): UaiConnectionDetailModel {
        return {
            unique: response.id,
            entityType: UAI_CONNECTION_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            providerId: response.providerId,
            settings: (response.settings as Record<string, unknown>) ?? null,
            isActive: response.isActive,
            version: response.version,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toItemModel(response: ConnectionItemResponseModel): UaiConnectionItemModel {
        return {
            unique: response.id,
            entityType: UAI_CONNECTION_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            providerId: response.providerId,
            isActive: response.isActive,
            dateModified: response.dateModified,
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

    toModelDescriptorModel(response: ModelDescriptorResponseModel): UaiModelDescriptorModel {
        return {
            model: {
                providerId: response.model.providerId,
                modelId: response.model.modelId,
            },
            name: response.name,
            metadata: response.metadata ?? undefined,
        };
    },
};
