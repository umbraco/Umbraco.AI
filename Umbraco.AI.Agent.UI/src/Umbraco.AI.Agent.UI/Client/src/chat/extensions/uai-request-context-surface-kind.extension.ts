import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

/**
 * Agent surface kind for uaiRequestContextContributor extension type.
 *
 * Provides the generic agent surface contributor API for manifests
 * that declare `kind: "agentSurface"`. Surfaces only need to provide
 * `meta: { surface: "<name>" }` — no custom API required.
 */
export const UAI_REQUEST_CONTEXT_AGENT_SURFACE_KIND_MANIFEST: UmbExtensionManifestKind = {
	type: "kind",
	alias: "Uai.Kind.RequestContextContributor.AgentSurface",
	matchKind: "agentSurface",
	matchType: "uaiRequestContextContributor",
	manifest: {
		type: "uaiRequestContextContributor",
		kind: "agentSurface",
		// Type assertion needed: UmbExtensionManifestKind.manifest is Partial<ManifestBase>
		// which doesn't include `api` from ManifestApi. The extension registry merges
		// this at runtime and `api` is correctly resolved.
		api: () => import("./surface.contributor.js"),
	} as Record<string, unknown>,
};
