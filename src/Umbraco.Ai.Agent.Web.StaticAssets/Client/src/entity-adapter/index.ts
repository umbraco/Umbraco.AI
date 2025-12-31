/**
 * Entity Adapter Module
 *
 * Provides entity detection and serialization for AI tools to interact
 * with Umbraco entities being edited in the backoffice.
 *
 * @example
 * ```typescript
 * import { UaiEntityAdapterContext } from './entity-adapter';
 *
 * const entityAdapter = new UaiEntityAdapterContext();
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

// Adapters
export { UaiDocumentAdapter } from "./adapters/document.adapter.js";

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
