import type { UaiContextEntityType } from './entity.js';

/**
 * Injection modes for context resources.
 */
export type UaiContextResourceInjectionMode = 'Always' | 'OnDemand';

/**
 * Resource within a context.
 */
export interface UaiContextResourceModel {
    id: string;
    resourceTypeId: string;
    name: string;
    description: string | null;
    sortOrder: number;
    data: string;
    injectionMode: UaiContextResourceInjectionMode;
}

/**
 * Detail model for a context (full data for editing).
 */
export interface UaiContextDetailModel {
    unique: string;
    entityType: UaiContextEntityType;
    alias: string;
    name: string;
    resources: UaiContextResourceModel[];
}

/**
 * Item model for a context (summary for lists).
 */
export interface UaiContextItemModel {
    unique: string;
    entityType: UaiContextEntityType;
    alias: string;
    name: string;
    resourceCount: number;
}
