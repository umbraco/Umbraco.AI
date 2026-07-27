import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_KNOWLEDGE_SET_COLLECTION_REPOSITORY_ALIAS } from "./constants.js";

export const knowledgeSetRepositoryManifests: Array<ManifestRepository> = [
    {
        type: "repository",
        alias: UAI_KNOWLEDGE_SET_COLLECTION_REPOSITORY_ALIAS,
        name: "Knowledge Set Collection Repository",
        api: () => import("./collection/knowledge-set-collection.repository.js"),
    },
];
