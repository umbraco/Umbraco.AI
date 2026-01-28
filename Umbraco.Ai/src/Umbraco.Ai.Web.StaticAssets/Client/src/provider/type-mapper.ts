import type { ProviderItemResponseModel, ProviderResponseModel } from "../api/types.gen.js";
import type { UaiProviderDetailModel, UaiProviderItemModel } from "./types.js";
import { UaiCommonTypeMapper } from "../core/type-mapper.ts";

export const UaiProviderTypeMapper = {
    toItemModel(response: ProviderItemResponseModel): UaiProviderItemModel {
        return {
            id: response.id,
            name: response.name,
            capabilities: response.capabilities,
        };
    },

    toDetailModel(response: ProviderResponseModel): UaiProviderDetailModel {
        return {
            id: response.id,
            name: response.name,
            capabilities: response.capabilities,
            settingsSchema: UaiCommonTypeMapper.toEditableModelSchemaModel(response.settingsSchema ?? { fields: [] }),
        };
    }
};
