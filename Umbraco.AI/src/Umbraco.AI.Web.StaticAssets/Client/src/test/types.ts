import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Collection item model for test list view.
 */
export interface UaiTestItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    testTypeId: string;
    tags: string[];
    runCount: number;
    dateModified: string | null;
}
