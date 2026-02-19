import type {
    TestItemResponseModel,
    TestFeatureInfoModel,
    TestResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
} from "../api/types.gen.js";
import { UAI_TEST_ENTITY_TYPE } from "./constants.js";
import type { UaiTestItemModel, UaiTestFeatureItemModel, UaiTestDetailModel } from "./types.js";

export const UaiTestTypeMapper = {
    toDetailModel(response: TestResponseModel): UaiTestDetailModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description,
            testFeatureId: response.testFeatureId,
            testTargetId: (response as any).testTargetId || "",
            testCase: (response.testCase as Record<string, any>) || null,
            graders: response.graders,
            runCount: response.runCount,
            tags: response.tags,
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
            testCase: model.testCase,
            graders: model.graders,
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
            testCase: model.testCase,
            graders: model.graders,
            runCount: model.runCount,
            tags: model.tags,
        } as any; // Cast needed until API types are regenerated
    },
};
