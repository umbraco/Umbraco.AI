/**
 * Entity Adapter Module
 *
 * Provides entity detection and serialization for AI tools to interact
 * with Umbraco entities being edited in the backoffice.
 *
 * @example
 * ```typescript
 * import { UaiEntityAdapterContext } from '@umbraco-ai/core/entity-adapter';
 *
 * const entityAdapter = new UaiEntityAdapterContext(extensionRegistry);
 *
 * // Get all detected entities
 * entityAdapter.detectedEntities$.subscribe((entities) => {
 *   console.log('Detected entities:', entities);
 * });
 *
 * // Serialize selected entity for LLM context
 * const serialized = await entityAdapter.serializeSelectedEntity();
 * ```
 */

// Context
export { UaiEntityAdapterContext } from "./entity-adapter.context.js";

// Extension type
export { UAI_ENTITY_ADAPTER_EXTENSION_TYPE, type ManifestEntityAdapter } from "./extension-type.js";

// Adapters
export { UaiDocumentAdapter } from "./adapters/document.adapter.js";

// Manifests
export { entityAdapterManifests } from "./adapters/manifests.js";

// Types
export type {
	UaiDetectedEntity,
	UaiEntityAdapterApi,
	UaiEntityContext,
	UaiPropertyChange,
	UaiPropertyChangeResult,
	UaiSerializedEntity,
	UaiSerializedProperty,
} from "./types.js";
