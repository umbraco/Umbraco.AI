import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_PROFILE_DETAIL_REPOSITORY_ALIAS,
    UAI_PROFILE_DETAIL_STORE_ALIAS,
    UAI_PROFILE_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const profileRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_PROFILE_DETAIL_REPOSITORY_ALIAS,
        name: "Profile Detail Repository",
        api: () => import("./detail/profile-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_PROFILE_DETAIL_STORE_ALIAS,
        name: "Profile Detail Store",
        api: () => import("./detail/profile-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_PROFILE_COLLECTION_REPOSITORY_ALIAS,
        name: "Profile Collection Repository",
        api: () => import("./collection/profile-collection.repository.js"),
    },
];
