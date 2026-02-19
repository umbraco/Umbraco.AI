import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { TestTargetModel, TestGraderModel } from "../api/types.gen.js";

/**
 * Detail model for test workspace editing.
 */
export interface UaiTestDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description?: string | null;
    testFeatureId: string;
    target: TestTargetModel;
    testCaseJson: string;
    graders: TestGraderModel[];
    runCount: number;
    tags: string[];
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

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
