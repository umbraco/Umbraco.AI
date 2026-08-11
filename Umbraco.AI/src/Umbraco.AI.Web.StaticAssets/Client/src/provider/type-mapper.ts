import type { ProviderItemResponseModel, ProviderResponseModel } from "../api/types.gen.js";
import type { UaiProviderDetailModel, UaiProviderItemModel } from "./types.js";
import type { UaiEditableModelSchemaModel } from "../core/types.js";
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
        const capabilitySettingsSchemas: Record<string, UaiEditableModelSchemaModel> = {};
        for (const [capability, schema] of Object.entries(response.capabilitySettingsSchemas ?? {})) {
            capabilitySettingsSchemas[capability] = UaiCommonTypeMapper.toEditableModelSchemaModel(schema);
        }

        return {
            id: response.id,
            name: response.name,
            capabilities: response.capabilities,
            settingsSchema: UaiCommonTypeMapper.toEditableModelSchemaModel(response.settingsSchema ?? { fields: [] }),
            capabilitySettingsSchemas,
        };
    },
};
