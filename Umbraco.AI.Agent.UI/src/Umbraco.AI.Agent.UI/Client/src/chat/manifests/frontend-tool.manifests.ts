import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiFrontendToolRepositoryApi } from "@umbraco-ai/core";

const frontendToolRepositoryManifest: ManifestRepository<UaiFrontendToolRepositoryApi> = {
    type: "repository",
    alias: "Uai.Repository.FrontendTool",
    name: "Frontend Tool Repository",
    api: () => import("../services/frontend-tool.repository.js"),
};

export const manifests = [frontendToolRepositoryManifest];
