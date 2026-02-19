import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiTestFeatureEntityRepositoryApi } from "../test-feature-entity-repository.js";

/**
 * Manifest for registering a test feature entity repository.
 *
 * Each test feature type (e.g., "prompt-completion", "agent-tool-test")
 * registers one repository that provides entities for that feature.
 *
 * @example
 * ```typescript
 * const manifest: ManifestUaiTestFeatureEntityRepository = {
 *     type: "repository",
 *     alias: "Uai.Repository.TestFeatureEntity.Prompt",
 *     name: "Prompt Test Feature Entity Repository",
 *     meta: {
 *         testFeatureType: "prompt-completion",
 *     },
 *     api: () => import("./prompt-test-entity.repository.js"),
 * };
 * ```
 */
export interface ManifestUaiTestFeatureEntityRepository
	extends ManifestRepository<UaiTestFeatureEntityRepositoryApi> {
	meta: {
		/** The test feature type ID this repository provides entities for */
		testFeatureType: string;
	};
}
