import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import { UaiConnectionConstants } from "../constants.js";

export const connectionRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UaiConnectionConstants.Repository.Detail,
        name: "Connection Detail Repository",
        api: () => import("./detail/connection-detail.repository.js"),
    },
    {
        type: "store",
        alias: UaiConnectionConstants.Store.Detail,
        name: "Connection Detail Store",
        api: () => import("./detail/connection-detail.store.js"),
    },
    {
        type: "repository",
        alias: UaiConnectionConstants.Repository.Collection,
        name: "Connection Collection Repository",
        api: () => import("./collection/connection-collection.repository.js"),
    },
];
