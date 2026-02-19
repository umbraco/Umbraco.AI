import type { TestItemResponseModel } from "../api/types.gen.js";
import { UAI_TEST_ENTITY_TYPE } from "./constants.js";
import type { UaiTestItemModel } from "./types.js";

export const UaiTestTypeMapper = {
    toItemModel(response: TestItemResponseModel): UaiTestItemModel {
        return {
            unique: response.id,
            entityType: UAI_TEST_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            testTypeId: response.testTypeId,
            tags: response.tags,
            runCount: response.runCount,
            dateModified: response.dateModified,
        };
    },
};
