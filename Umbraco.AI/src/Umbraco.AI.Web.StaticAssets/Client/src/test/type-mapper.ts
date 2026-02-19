import type { TestItemResponseModel, TestFeatureInfoModel } from "../api/types.gen.js";
import { UAI_TEST_ENTITY_TYPE } from "./constants.js";
import type { UaiTestItemModel, UaiTestFeatureItemModel } from "./types.js";

export const UaiTestTypeMapper = {
    toItemModel(response: TestItemResponseModel): UaiTestItemModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            testFeatureId: response.testTypeId,
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
};
