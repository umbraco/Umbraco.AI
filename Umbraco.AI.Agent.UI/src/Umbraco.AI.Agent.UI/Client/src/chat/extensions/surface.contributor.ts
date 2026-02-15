import type { UaiRequestContextContributorApi, UaiRequestContext } from "@umbraco-ai/core";

/**
 * Generic agent surface request context contributor.
 *
 * Reads the surface name from the manifest's `meta.surface` and
 * contributes an agent surface context item. Reusable by any chat surface
 * via the `kind: "agentSurface"` manifest kind.
 *
 * @example Registration via kind:
 * ```typescript
 * const manifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     kind: "agentSurface",
 *     alias: "UmbracoAI.Copilot.RequestContextContributor.Surface",
 *     name: "Copilot Agent Surface Contributor",
 *     meta: { surface: "copilot" },
 * };
 * ```
 */
export default class UaiAgentSurfaceRequestContextContributor implements UaiRequestContextContributorApi {
	async contribute(context: UaiRequestContext, meta?: Record<string, unknown>): Promise<void> {
		const surface = meta?.surface;
		if (typeof surface !== "string" || !surface) return;

		context.add({
			description: "agent surface",
			value: JSON.stringify({ surface }),
		});
	}

	destroy(): void {
		/* no-op */
	}
}
