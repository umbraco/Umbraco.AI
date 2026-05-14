/**
 * Entity Adapter Exports
 *
 * Public API exports for the entity adapter module.
 */

export {
    UaiEntityAdapterContext,
    UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
    UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
    UaiDocumentAdapter,
    resolveEntityAdapterByType,
    hasEntityAdapter,
    resolveAndPrepareValue,
    type ManifestEntityAdapter,
    type ManifestUaiPropertyValuePreparer,
    type UaiPropertyValuePreparerApi,
    type UaiDetectedEntity,
    type UaiEntityAdapterApi,
    type UaiEntityContext,
    type UaiValueChange,
    type UaiValueChangeResult,
    type UaiSerializedEntity,
    type UaiSerializedProperty,
} from "./index.js";
