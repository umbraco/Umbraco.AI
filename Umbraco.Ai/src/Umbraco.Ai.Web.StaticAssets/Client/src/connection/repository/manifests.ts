import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_CONNECTION_DETAIL_REPOSITORY_ALIAS,
    UAI_CONNECTION_DETAIL_STORE_ALIAS,
    UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
    UAI_CONNECTION_CAPABILITY_REPOSITORY_ALIAS,
} from "./constants.js";

export const connectionRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_CONNECTION_DETAIL_REPOSITORY_ALIAS,
        name: "Connection Detail Repository",
        api: () => import("./detail/connection-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_CONNECTION_DETAIL_STORE_ALIAS,
        name: "Connection Detail Store",
        api: () => import("./detail/connection-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
        name: "Connection Collection Repository",
        api: () => import("./collection/connection-collection.repository.js"),
    },
    {
        type: "repository",
        alias: UAI_CONNECTION_CAPABILITY_REPOSITORY_ALIAS,
        name: "Connection Capability Repository",
        api: () => import("./capability/connection-capability.repository.js"),
    },
];
