import type { TestResponseModel } from "./entity.js";
import { UAI_TEST_ENTITY_TYPE } from "./constants.js";
import type { UaiTestDetailModel, UaiTestItemModel } from "./types.js";

export const UaiTestTypeMapper = {
    toDetailModel(response: TestResponseModel): UaiTestDetailModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            testTypeId: response.testTypeId,
            target: response.target,
            testCase: response.testCase,
            graders: response.graders,
            runCount: response.runCount,
            tags: response.tags ?? [],
            isEnabled: response.isEnabled,
            baselineRunId: response.baselineRunId ?? null,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            createdByUserId: response.createdByUserId ?? null,
            modifiedByUserId: response.modifiedByUserId ?? null,
            version: response.version,
        };
    },

    toItemModel(response: TestResponseModel): UaiTestItemModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            testTypeId: response.testTypeId,
            isEnabled: response.isEnabled,
            runCount: response.runCount,
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiTestDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            description: model.description,
            testTypeId: model.testTypeId,
            target: model.target,
            testCase: model.testCase,
            graders: model.graders,
            runCount: model.runCount,
            tags: model.tags,
            isEnabled: model.isEnabled,
        };
    },

    toUpdateRequest(model: UaiTestDetailModel) {
        return {
            name: model.name,
            description: model.description,
            testTypeId: model.testTypeId,
            target: model.target,
            testCase: model.testCase,
            graders: model.graders,
            runCount: model.runCount,
            tags: model.tags,
            isEnabled: model.isEnabled,
        };
    },
};
