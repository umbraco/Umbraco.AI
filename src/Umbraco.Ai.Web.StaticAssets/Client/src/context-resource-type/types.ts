/**
 * ContextResourceType item model for UI consumption.
 * Maps from API's ContextResourceTypeItemResponseModel.
 */
export interface UaiContextResourceTypeItemModel {
    id: string;
    name: string;
    description?: string | null;
    icon?: string | null;
}

/**
 * ContextResourceType detail model for UI consumption.
 * Maps from API's ContextResourceTypeResponseModel.
 * Includes full contextResourceType information with setting definitions.
 */
export interface UaiContextResourceTypeDetailModel {
    id: string;
    name: string;
    description?: string | null;
    icon?: string | null;
}
