import type { ManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";
import type { UaiRequestContextItem } from "./types.js";

/**
 * Extension type alias for request context contributors.
 */
export const UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE = "uaiRequestContextContributor";

/**
 * Mutable context bag passed to each contributor.
 * Mirrors the backend AIRuntimeContext pattern -- contributors
 * call `add()` to push context items rather than returning them.
 */
export class UaiRequestContext {
	readonly #items: UaiRequestContextItem[] = [];

	/**
	 * Add a context item to the request context.
	 */
	add(item: UaiRequestContextItem): void {
		this.#items.push(item);
	}

	/**
	 * Get all contributed context items.
	 */
	getItems(): UaiRequestContextItem[] {
		return [...this.#items];
	}
}

/**
 * API interface for request context contributors.
 * Implement this to contribute context items to AI requests.
 *
 * Frontend counterpart of the backend IAIRuntimeContextContributor.
 * While the backend contributors *process* request context items into
 * runtime context, frontend contributors *produce* the request context
 * items that get sent in the request.
 *
 * Mirrors the backend signature: `void Contribute(AIRuntimeContext context)`
 */
export interface UaiRequestContextContributorApi extends UmbApi {
	/**
	 * Contribute context items to the request context.
	 * Called once per message send / prompt execution.
	 * Add items via `context.add(item)`. No-op to contribute nothing.
	 *
	 * @param context The mutable request context to contribute to.
	 * @param meta Optional manifest meta – passed from the manifest's `meta`
	 *             property so kind-based contributors can read configuration
	 *             without hardcoding values.
	 */
	contribute(context: UaiRequestContext, meta?: Record<string, unknown>): Promise<void>;
}

/**
 * Manifest for request context contributor extensions.
 *
 * Request context contributors are invoked before each AI request to gather
 * ambient context (current section, entity, surface, etc.) into
 * UaiRequestContextItem[] for the backend.
 *
 * Core contributors are unconditional (always run).
 * Product-specific contributors can use Umbraco's conditions framework
 * to gate when they contribute.
 *
 * @example
 * ```typescript
 * // Unconditional contributor (always contributes)
 * const manifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     alias: "UmbracoAI.RequestContextContributor.Section",
 *     name: "Section Request Context Contributor",
 *     api: () => import("./section.contributor.js"),
 *     weight: 100,
 * };
 *
 * // Kind-based contributor (api provided by kind, meta configures it)
 * const surfaceManifest: ManifestUaiRequestContextContributor = {
 *     type: "uaiRequestContextContributor",
 *     kind: "surface",
 *     alias: "UmbracoAI.Copilot.RequestContextContributor.Surface",
 *     name: "Copilot Surface Request Context Contributor",
 *     meta: { surface: "copilot" },
 * };
 * ```
 */
export interface ManifestUaiRequestContextContributor extends ManifestApi<UaiRequestContextContributorApi> {
	type: typeof UAI_REQUEST_CONTEXT_CONTRIBUTOR_EXTENSION_TYPE;
	/** Optional metadata passed to the contributor's `contribute()` method. */
	meta?: Record<string, unknown>;
}

declare global {
	interface UmbExtensionManifestMap {
		uaiRequestContextContributor: ManifestUaiRequestContextContributor;
	}
}
