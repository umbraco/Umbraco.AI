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

// Value preparer plugin surface
export {
    UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
    resolveAndPrepareValue,
    type ManifestUaiPropertyValuePreparer,
    type UaiPropertyValuePreparerApi,
} from "./value-preparers/index.js";

// Extension type
export { UAI_ENTITY_ADAPTER_EXTENSION_TYPE, type ManifestEntityAdapter } from "./extension-type.js";

// Helpers
export { resolveEntityAdapterByType, hasEntityAdapter } from "./helpers.js";

// Adapters
export { UaiDocumentAdapter } from "./adapters/document.adapter.js";
export { UaiMediaAdapter } from "./adapters/media.adapter.js";

// Types
export type {
    UaiDetectedEntity,
    UaiEntityAdapterApi,
    UaiEntityContext,
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
    UaiSerializedProperty,
} from "./types.js";
