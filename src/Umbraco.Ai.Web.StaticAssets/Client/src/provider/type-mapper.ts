import type { ProviderItemResponseModel, ProviderResponseModel, SettingDefinitionModel } from "../api/types.gen.js";
import type { UaiProviderDetailModel, UaiProviderItemModel, UaiSettingDefinitionModel } from "./types.js";

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
            settingDefinitions: response.settingDefinitions.map(UaiProviderTypeMapper.toSettingDefinitionModel),
        };
    },

    toSettingDefinitionModel(response: SettingDefinitionModel): UaiSettingDefinitionModel {
        return {
            key: response.key,
            label: response.label,
            description: response.description ?? undefined,
            editorUiAlias: response.editorUiAlias ?? undefined,
            editorConfig: response.editorConfig ?? undefined,
            defaultValue: response.defaultValue ?? undefined,
            sortOrder: response.sortOrder,
            isRequired: response.isRequired,
        };
    },
};
