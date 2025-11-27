/**
 * Provider item model for UI consumption.
 * Maps from API's ProviderItemResponseModel.
 */
export interface UaiProviderItemModel {
    id: string;
    name: string;
    capabilities: string[];
}

/**
 * Setting definition model for UI consumption.
 * Maps from API's SettingDefinitionModel.
 */
export interface UaiSettingDefinitionModel {
    key: string;
    label: string;
    description?: string;
    editorUiAlias?: string;
    editorConfig?: unknown;
    defaultValue?: unknown;
    sortOrder: number;
    isRequired: boolean;
}

/**
 * Provider detail model for UI consumption.
 * Maps from API's ProviderResponseModel.
 * Includes full provider information with setting definitions.
 */
export interface UaiProviderDetailModel {
    id: string;
    name: string;
    capabilities: string[];
    settingDefinitions: UaiSettingDefinitionModel[];
}
