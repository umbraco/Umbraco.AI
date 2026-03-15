import type { ManifestRepository, ManifestStore } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_GUARDRAIL_DETAIL_REPOSITORY_ALIAS,
    UAI_GUARDRAIL_DETAIL_STORE_ALIAS,
    UAI_GUARDRAIL_COLLECTION_REPOSITORY_ALIAS,
} from "./constants.js";

export const guardrailRepositoryManifests: Array<ManifestRepository | ManifestStore> = [
    {
        type: "repository",
        alias: UAI_GUARDRAIL_DETAIL_REPOSITORY_ALIAS,
        name: "Guardrail Detail Repository",
        api: () => import("./detail/guardrail-detail.repository.js"),
    },
    {
        type: "store",
        alias: UAI_GUARDRAIL_DETAIL_STORE_ALIAS,
        name: "Guardrail Detail Store",
        api: () => import("./detail/guardrail-detail.store.js"),
    },
    {
        type: "repository",
        alias: UAI_GUARDRAIL_COLLECTION_REPOSITORY_ALIAS,
        name: "Guardrail Collection Repository",
        api: () => import("./collection/guardrail-collection.repository.js"),
    },
];
