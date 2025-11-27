import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

// View model for workspace editing
export interface UaiConnectionDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    providerId: string;
    settings: Record<string, unknown> | null;
    isActive: boolean;
}

// Collection item model
export interface UaiConnectionItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    name: string;
    providerId: string;
    isActive: boolean;
}
