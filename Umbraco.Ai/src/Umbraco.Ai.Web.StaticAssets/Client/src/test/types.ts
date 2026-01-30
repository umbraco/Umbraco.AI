import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { TestResponseModel, TestGraderModel, TestTargetModel, TestCaseModel } from "./entity.js";

/**
 * Detail model for test workspace editing.
 */
export interface UaiTestDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    testTypeId: string;
    target: TestTargetModel;
    testCase: TestCaseModel;
    graders: TestGraderModel[];
    runCount: number;
    tags: string[];
    isEnabled: boolean;
    baselineRunId: string | null;
    dateCreated: string | null;
    dateModified: string | null;
    createdByUserId: string | null;
    modifiedByUserId: string | null;
    version: number;
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UaiTestItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    testTypeId: string;
    isEnabled: boolean;
    runCount: number;
    dateModified: string | null;
}
