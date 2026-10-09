import type {
    ConnectionResponseModel,
    ConnectionItemResponseModel,
    ModelDescriptorResponseModel,
    ModelFactResponseModel,
    ModelFactsItemResponseModel,
} from "../api/types.gen.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "./constants.js";
import type {
    UaiConnectionDetailModel,
    UaiConnectionItemModel,
    UaiModelDescriptorModel,
    UaiModelFactModel,
    UaiModelFactsModel,
} from "./types.js";

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
            // Model is [Required] server-side; the nullable schema type is a model-binding artifact.
            model: {
                providerId: response.model!.providerId,
                modelId: response.model!.modelId,
            },
            name: response.name,
            metadata: response.metadata ?? undefined,
        };
    },

    toModelFactModel(response: ModelFactResponseModel): UaiModelFactModel {
        return {
            key: response.key,
            label: response.label,
            shortLabel: response.shortLabel ?? undefined,
            value: response.value,
            sortValue: response.sortValue ?? undefined,
            detail: response.detail ?? undefined,
            tone: response.tone,
            url: response.url ?? undefined,
        };
    },

    toModelFactsModel(response: ModelFactsItemResponseModel): UaiModelFactsModel {
        return {
            model: {
                providerId: response.model.providerId,
                modelId: response.model.modelId,
            },
            facts: response.facts.map(UaiConnectionTypeMapper.toModelFactModel),
        };
    },
};
