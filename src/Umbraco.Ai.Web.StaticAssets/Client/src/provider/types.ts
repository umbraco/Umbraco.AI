/**
 * Provider item model for UI consumption.
 * Maps from API's ProviderItemResponseModel.
 */
export interface UaiProviderItemModel {
    id: string;
    name: string;
    capabilities: string[];
}
