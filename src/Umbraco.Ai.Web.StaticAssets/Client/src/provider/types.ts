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

/**
 * Model reference for UI consumption.
 * Maps from API's ModelRefModel.
 */
export interface UaiModelRefModel {
    providerId: string;
    modelId: string;
}

/**
 * Model descriptor for UI consumption.
 * Maps from API's ModelDescriptorResponseModel.
 */
export interface UaiModelDescriptorModel {
    model: UaiModelRefModel;
    name: string;
    metadata?: Record<string, string>;
}
