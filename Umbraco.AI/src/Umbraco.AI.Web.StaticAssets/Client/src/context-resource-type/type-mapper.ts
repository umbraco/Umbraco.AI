import type { UaiContextResourceTypeDetailModel, UaiContextResourceTypeItemModel } from "./types.js";
import { ContextResourceTypeResponseModel } from "../api";
import { UaiCommonTypeMapper } from "../core/type-mapper.js";

export const UaiContextResourceTypeTypeMapper = {
    toItemModel(response: ContextResourceTypeResponseModel): UaiContextResourceTypeItemModel {
        return {
            id: response.id,
            name: response.name,
            description: response.description,
            icon: response.icon,
            settingsSchema: response.settingsSchema
                ? UaiCommonTypeMapper.toEditableModelSchemaModel(response.settingsSchema)
                : null,
        };
    },

    toDetailModel(response: ContextResourceTypeResponseModel): UaiContextResourceTypeDetailModel {
        return {
            id: response.id,
            name: response.name,
            description: response.description,
            icon: response.icon,
            settingsSchema: response.settingsSchema
                ? UaiCommonTypeMapper.toEditableModelSchemaModel(response.settingsSchema)
                : null,
        };
    },
};
