import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiFrontendToolRepositoryApi } from "@umbraco-ai/core";
import { manifests as entityToolManifests } from "./entity/manifests.js";
import { manifests as exampleToolManifests } from "./examples/manifests.js";
import { manifests as umbracoToolManifests } from "./umbraco/manifests.js";

const frontendToolRepositoryManifest: ManifestRepository<UaiFrontendToolRepositoryApi> = {
    type: "repository",
    alias: "Uai.Repository.FrontendTool",
    name: "Frontend Tool Repository",
    api: () => import("./frontend-tool.repository.js"),
};

export const manifests = [
    frontendToolRepositoryManifest,
    ...entityToolManifests,
    ...exampleToolManifests,
    ...umbracoToolManifests,
];
