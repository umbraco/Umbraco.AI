import type { ManifestUaiTestFeatureEntityRepository } from "@umbraco-ai/core";

const testEntityRepositoryManifest: ManifestUaiTestFeatureEntityRepository = {
	type: "repository",
	alias: "Uai.Repository.TestFeatureEntity.Agent",
	name: "Agent Test Feature Entity Repository",
	meta: {
		testFeatureType: "agent-tool-test", // Must match backend feature ID
	},
	api: () => import("./agent-test-entity.repository.js"),
};

export const manifests = [testEntityRepositoryManifest];
