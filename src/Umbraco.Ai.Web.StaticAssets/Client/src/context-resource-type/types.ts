import type { UaiEditableModelSchemaModel } from "../core/types.js";

/**
 * ContextResourceType item model for UI consumption.
 * Lightweight model for lists and selection.
 * Maps from API's ContextResourceTypeItemResponseModel.
 */
export interface UaiContextResourceTypeItemModel {
    id: string;
    name: string;
    description?: string | null;
    icon?: string | null;
    dataSchema?: UaiEditableModelSchemaModel | null;
}

/**
 * ContextResourceType detail model for UI consumption.
 * Maps from API's ContextResourceTypeResponseModel.
 * Includes full contextResourceType information with data schema.
 */
export interface UaiContextResourceTypeDetailModel {
    id: string;
    name: string;
    description?: string | null;
    icon?: string | null;
    dataSchema?: UaiEditableModelSchemaModel | null;
}
