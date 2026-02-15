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
    contribute(context: UaiRequestContext, meta?: Record<string, unknown>): Promise<void>;
    destroy(): void;
}
//# sourceMappingURL=surface.contributor.d.ts.map