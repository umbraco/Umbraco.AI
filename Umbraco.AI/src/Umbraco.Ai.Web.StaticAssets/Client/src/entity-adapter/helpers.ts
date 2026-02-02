/**
 * Entity Adapter Helpers
 *
 * Helper functions for resolving entity adapters dynamically.
 */

import { loadManifestApi } from "@umbraco-cms/backoffice/extension-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_ENTITY_ADAPTER_EXTENSION_TYPE, type ManifestEntityAdapter } from "./extension-type.js";
import type { UaiEntityAdapterApi } from "./types.js";

/**
 * Cache for loaded adapter instances by manifest alias.
 * Shared across all consumers to avoid loading the same adapter multiple times.
 */
const adapterCache = new Map<string, UaiEntityAdapterApi>();

/**
 * Resolve an entity adapter by entity type.
 *
 * Looks up the extension registry for an adapter manifest matching the entity type,
 * loads the adapter API module, and returns an instance.
 *
 * @param entityType - The entity type to find an adapter for (e.g., "document", "media")
 * @returns The adapter instance, or undefined if no matching adapter is found
 *
 * @example
 * ```typescript
 * const entityType = workspaceContext.getEntityType();
 * const adapter = await resolveEntityAdapterByType(entityType);
 * if (adapter) {
 *   const serialized = await adapter.serializeForLlm(workspaceContext);
 * }
 * ```
 */
export async function resolveEntityAdapterByType(
	entityType: string,
): Promise<UaiEntityAdapterApi | undefined> {
	// Find adapter manifest by entity type
	const manifests = umbExtensionsRegistry.getByType(
		UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
	) as ManifestEntityAdapter[];

	const manifest = manifests.find((m) => m.forEntityType === entityType);
	if (!manifest) {
		return undefined;
	}

	// Check cache first
	let adapter = adapterCache.get(manifest.alias);
	if (adapter) {
		return adapter;
	}

	// Load the adapter API module
	try {
		const ApiModule = await loadManifestApi(manifest.api);
		if (ApiModule) {
			const ApiClass = (ApiModule as any).default ?? ApiModule;
			adapter = new ApiClass() as UaiEntityAdapterApi;
			adapterCache.set(manifest.alias, adapter);
			return adapter;
		}
	} catch (e) {
		console.error(
			`[resolveEntityAdapterByType] Failed to load adapter for entity type "${entityType}":`,
			e,
		);
	}

	return undefined;
}

/**
 * Check if an entity adapter is registered for the given entity type.
 *
 * @param entityType - The entity type to check (e.g., "document", "media")
 * @returns True if an adapter is registered for this entity type
 */
export function hasEntityAdapter(entityType: string): boolean {
	const manifests = umbExtensionsRegistry.getByType(
		UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
	) as ManifestEntityAdapter[];

	return manifests.some((m) => m.forEntityType === entityType);
}
