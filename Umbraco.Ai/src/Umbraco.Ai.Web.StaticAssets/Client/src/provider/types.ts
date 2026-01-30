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
}
