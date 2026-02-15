import type { UaiRequestContextContributorApi, UaiRequestContext } from "@umbraco-ai/core";

/**
 * Generic surface request context contributor.
 *
 * Reads the surface name from the manifest's `meta.surface` and
 * contributes a surface context item. Reusable by any chat surface
 * via the `kind: "surface"` manifest kind.
 *
 * @example Registration via kind:
 * ```typescript
 * const manifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     kind: "surface",
 *     alias: "UmbracoAI.Copilot.RequestContextContributor.Surface",
 *     name: "Copilot Surface Contributor",
 *     meta: { surface: "copilot" },
 * };
 * ```
 */
export default class UaiSurfaceRequestContextContributor implements UaiRequestContextContributorApi {
	async contribute(context: UaiRequestContext, meta?: Record<string, unknown>): Promise<void> {
		const surface = meta?.surface;
		if (typeof surface !== "string" || !surface) return;

		context.add({
			description: "surface",
			value: JSON.stringify({ surface }),
		});
	}

	destroy(): void {
		/* no-op */
	}
}
