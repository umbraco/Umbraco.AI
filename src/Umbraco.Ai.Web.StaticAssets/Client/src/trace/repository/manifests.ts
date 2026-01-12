import {
    UAI_TRACE_DETAIL_REPOSITORY_ALIAS,
    UAI_TRACE_DETAIL_STORE_ALIAS,
    UAI_TRACE_COLLECTION_REPOSITORY_ALIAS,
} from "../constants.js";

export const traceRepositoryManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: UAI_TRACE_DETAIL_REPOSITORY_ALIAS,
        name: "Trace Detail Repository",
        api: () => import("./detail/trace-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_TRACE_DETAIL_STORE_ALIAS,
        name: "Trace Detail Store",
        api: () => import("./detail/trace-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_TRACE_COLLECTION_REPOSITORY_ALIAS,
        name: "Trace Collection Repository",
        api: () => import("./collection/trace-collection.repository.js"),
    },
];
