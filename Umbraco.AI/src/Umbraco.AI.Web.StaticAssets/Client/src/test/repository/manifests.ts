import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_TEST_COLLECTION_REPOSITORY_ALIAS,
    UAI_TEST_DETAIL_REPOSITORY_ALIAS,
    UAI_TEST_DETAIL_STORE_ALIAS,
    UAI_TEST_RUN_COLLECTION_REPOSITORY_ALIAS,
    UAI_TEST_EXECUTION_REPOSITORY_ALIAS,
    UAI_TEST_RUN_DETAIL_REPOSITORY_ALIAS,
    UAI_TEST_GRADER_ITEM_REPOSITORY_ALIAS,
} from "../constants.js";

export const testRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_TEST_DETAIL_REPOSITORY_ALIAS,
        name: "Test Detail Repository",
        api: () => import("./detail/test-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_TEST_DETAIL_STORE_ALIAS,
        name: "Test Detail Store",
        api: () => import("./detail/test-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_TEST_COLLECTION_REPOSITORY_ALIAS,
        name: "Test Collection Repository",
        api: () => import("./collection/test-collection.repository.js"),
    },
    {
        type: "repository",
        alias: UAI_TEST_RUN_COLLECTION_REPOSITORY_ALIAS,
        name: "Test Run Collection Repository",
        api: () => import("./test-run-collection/test-run-collection.repository.js"),
    },
    {
        type: "repository",
        alias: UAI_TEST_EXECUTION_REPOSITORY_ALIAS,
        name: "Test Execution Repository",
        api: () => import("./test-execution/test-execution.repository.js"),
    },
    {
        type: "repository",
        alias: UAI_TEST_RUN_DETAIL_REPOSITORY_ALIAS,
        name: "Test Run Detail Repository",
        api: () => import("./test-run-detail/test-run-detail.repository.js"),
    },
    {
        type: "repository",
        alias: UAI_TEST_GRADER_ITEM_REPOSITORY_ALIAS,
        name: "Test Grader Item Repository",
        api: () => import("./test-grader/test-grader-item.repository.js"),
    },
];
