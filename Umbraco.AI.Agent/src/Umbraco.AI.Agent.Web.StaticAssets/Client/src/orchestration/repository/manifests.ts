import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_ORCHESTRATION_DETAIL_REPOSITORY_ALIAS,
    UAI_ORCHESTRATION_DETAIL_STORE_ALIAS,
    UAI_ORCHESTRATION_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const orchestrationRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_ORCHESTRATION_DETAIL_REPOSITORY_ALIAS,
        name: "Orchestration Detail Repository",
        api: () => import("./detail/orchestration-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_ORCHESTRATION_DETAIL_STORE_ALIAS,
        name: "Orchestration Detail Store",
        api: () => import("./detail/orchestration-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_ORCHESTRATION_COLLECTION_REPOSITORY_ALIAS,
        name: "Orchestration Collection Repository",
        api: () => import("./collection/orchestration-collection.repository.js"),
    },
];
