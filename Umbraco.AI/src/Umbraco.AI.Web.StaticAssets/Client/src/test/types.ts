import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Collection item model for test list view.
 */
export interface UaiTestItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    testFeatureId: string;
    tags: string[];
    runCount: number;
    dateModified: string | null;
}

/**
 * Test feature item model for feature selection.
 */
export interface UaiTestFeatureItemModel {
    id: string;
    name: string;
    description?: string | null;
    category?: string | null;
}
