import type {
    TestItemResponseModel,
    TestFeatureInfoModel,
    TestResponseModel,
    TestRunResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
} from "../api/types.gen.js";
import { UAI_TEST_ENTITY_TYPE, UAI_TEST_RUN_ENTITY_TYPE } from "./constants.js";
import type { UaiTestItemModel, UaiTestFeatureItemModel, UaiTestDetailModel, UaiTestRunItemModel, UaiTestVariation } from "./types.js";

export const UaiTestTypeMapper = {
    toDetailModel(response: TestResponseModel): UaiTestDetailModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description,
            testFeatureId: response.testFeatureId,
            testTargetId: response.testTargetId || "",
            profileId: (response as any).profileId ?? null,
            contextIds: (response as any).contextIds ?? [],
            testFeatureConfig: (response.testFeatureConfig as Record<string, any>) || null,
            graders: response.graders,
            variations: ((response as any).variations ?? []).map((v: any) => ({
                id: v.id,
                name: v.name,
                description: v.description ?? null,
                profileId: v.profileId ?? null,
                runCount: v.runCount ?? null,
                contextIds: v.contextIds ?? null,
                testFeatureConfig: v.testFeatureConfig ?? null,
            } as UaiTestVariation)),
            runCount: response.runCount,
            tags: response.tags,
            baselineRunId: response.baselineRunId ?? null,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version,
        };
    },

    toItemModel(response: TestItemResponseModel): UaiTestItemModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            testFeatureId: response.testFeatureId,
            tags: response.tags,
            runCount: response.runCount,
            dateModified: response.dateModified,
        };
    },

    toRunItemModel(response: TestRunResponseModel): UaiTestRunItemModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_RUN_ENTITY_TYPE,
            testId: response.testId,
            testName: response.testName,
            runNumber: response.runNumber,
            status: response.status,
            durationMs: response.durationMs,
            executedAt: response.executedAt,
            batchId: response.batchId,
            executionId: (response as any).executionId ?? null,
            variationId: (response as any).variationId ?? null,
            variationName: (response as any).variationName ?? null,
            profileId: response.profileId,
            isBaseline: response.isBaseline,
            baselineRunId: response.baselineRunId ?? null,
        };
    },

    toTestFeatureItemModel(response: TestFeatureInfoModel): UaiTestFeatureItemModel {
        return {
            id: response.id,
            name: response.name,
            description: response.description,
            category: response.category,
        };
    },

    toCreateRequest(model: UaiTestDetailModel): CreateTestRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            testFeatureId: model.testFeatureId,
            testTargetId: model.testTargetId,
            profileId: model.profileId || undefined,
            contextIds: model.contextIds.length > 0 ? model.contextIds : undefined,
            testFeatureConfig: model.testFeatureConfig,
            graders: model.graders,
            variations: model.variations.length > 0 ? model.variations.map(v => ({
                id: v.id,
                name: v.name,
                description: v.description,
                profileId: v.profileId || undefined,
                runCount: v.runCount ?? undefined,
                contextIds: v.contextIds ?? undefined,
                testFeatureConfig: v.testFeatureConfig ?? undefined,
            })) : undefined,
            runCount: model.runCount,
            tags: model.tags,
        } as any; // Cast needed until API types are regenerated
    },

    toUpdateRequest(model: UaiTestDetailModel): UpdateTestRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            testTargetId: model.testTargetId,
            profileId: model.profileId || undefined,
            contextIds: model.contextIds.length > 0 ? model.contextIds : undefined,
            testFeatureConfig: model.testFeatureConfig,
            graders: model.graders,
            variations: model.variations.length > 0 ? model.variations.map(v => ({
                id: v.id,
                name: v.name,
                description: v.description,
                profileId: v.profileId || undefined,
                runCount: v.runCount ?? undefined,
                contextIds: v.contextIds ?? undefined,
                testFeatureConfig: v.testFeatureConfig ?? undefined,
            })) : undefined,
            runCount: model.runCount,
            tags: model.tags,
        } as any; // Cast needed until API types are regenerated
    },
};
