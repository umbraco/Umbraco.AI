import type { ManifestRepository } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiTestFeatureEntityRepositoryApi } from "../test-feature-entity-repository.js";

/**
 * Manifest for registering a test feature entity repository.
 *
 * Each test feature (e.g., "prompt", "agent")
 * registers one repository that provides entities for that feature.
 *
 * @example
 * ```typescript
 * const manifest: ManifestUaiTestFeatureEntityRepository = {
 *     type: "repository",
 *     alias: "Uai.Repository.TestFeatureEntity.Prompt",
 *     name: "Prompt Test Feature Entity Repository",
 *     meta: {
 *         feature: "prompt",
 *     },
 *     api: () => import("./prompt-test-entity.repository.js"),
 * };
 * ```
 */
export interface ManifestUaiTestFeatureEntityRepository
	extends ManifestRepository<UaiTestFeatureEntityRepositoryApi> {
	meta: {
		/** The test feature ID this repository provides entities for */
		feature: string;
	};
}
