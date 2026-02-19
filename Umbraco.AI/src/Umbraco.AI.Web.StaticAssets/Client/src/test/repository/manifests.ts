import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_TEST_COLLECTION_REPOSITORY_ALIAS } from "../constants.js";

export const testRepositoryManifests: Array<ManifestRepository> = [
    {
        type: "repository",
        alias: UAI_TEST_COLLECTION_REPOSITORY_ALIAS,
        name: "Test Collection Repository",
        api: () => import("./collection/test-collection.repository.js"),
    },
];
