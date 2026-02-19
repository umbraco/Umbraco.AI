import type { ManifestUaiTestFeatureEntityRepository } from "@umbraco-ai/core";

const testEntityRepositoryManifest: ManifestUaiTestFeatureEntityRepository = {
	type: "repository",
	alias: "Uai.Repository.TestFeatureEntity.Prompt",
	name: "Prompt Test Feature Entity Repository",
	meta: {
		feature: "prompt", // Must match backend feature ID
	},
	api: () => import("./prompt-test-entity.repository.js"),
};

export const manifests = [testEntityRepositoryManifest];
