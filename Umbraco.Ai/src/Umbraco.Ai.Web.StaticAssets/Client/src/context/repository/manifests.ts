import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_CONTEXT_DETAIL_REPOSITORY_ALIAS,
    UAI_CONTEXT_DETAIL_STORE_ALIAS,
    UAI_CONTEXT_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const contextRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_CONTEXT_DETAIL_REPOSITORY_ALIAS,
        name: "Context Detail Repository",
        api: () => import("./detail/context-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_CONTEXT_DETAIL_STORE_ALIAS,
        name: "Context Detail Store",
        api: () => import("./detail/context-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_CONTEXT_COLLECTION_REPOSITORY_ALIAS,
        name: "Context Collection Repository",
        api: () => import("./collection/context-collection.repository.js"),
    },
];
