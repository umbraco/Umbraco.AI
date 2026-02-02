import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_AGENT_DETAIL_REPOSITORY_ALIAS,
    UAI_AGENT_DETAIL_STORE_ALIAS,
    UAI_AGENT_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const agentRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_AGENT_DETAIL_REPOSITORY_ALIAS,
        name: "Agent Detail Repository",
        api: () => import("./detail/agent-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_AGENT_DETAIL_STORE_ALIAS,
        name: "Agent Detail Store",
        api: () => import("./detail/agent-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_AGENT_COLLECTION_REPOSITORY_ALIAS,
        name: "Agent Collection Repository",
        api: () => import("./collection/agent-collection.repository.js"),
    },
];
