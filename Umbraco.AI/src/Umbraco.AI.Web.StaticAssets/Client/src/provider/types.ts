import type { UaiEditableModelSchemaModel } from "../core/types.js";

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
 * Provider detail model for UI consumption.
 * Maps from API's ProviderResponseModel.
 * Includes full provider information with setting definitions.
 */
export interface UaiProviderDetailModel {
    id: string;
    name: string;
    capabilities: string[];
    settingsSchema: UaiEditableModelSchemaModel;
    /**
     * Provider-declared profile-level settings schemas (e.g. reasoning effort), keyed by capability
     * name (e.g. "Chat"). Only capabilities that declare extra profile settings appear.
     */
    capabilitySettingsSchemas: Record<string, UaiEditableModelSchemaModel>;
}
