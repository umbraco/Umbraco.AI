import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_PROMPT_DETAIL_REPOSITORY_ALIAS,
    UAI_PROMPT_DETAIL_STORE_ALIAS,
    UAI_PROMPT_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const promptRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_PROMPT_DETAIL_REPOSITORY_ALIAS,
        name: "Prompt Detail Repository",
        api: () => import("./detail/prompt-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_PROMPT_DETAIL_STORE_ALIAS,
        name: "Prompt Detail Store",
        api: () => import("./detail/prompt-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_PROMPT_COLLECTION_REPOSITORY_ALIAS,
        name: "Prompt Collection Repository",
        api: () => import("./collection/prompt-collection.repository.js"),
    },
];
