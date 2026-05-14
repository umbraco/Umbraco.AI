import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import type { UaiPropertyValuePreparerApi } from "./types.js";

/**
 * Extension type alias for property value preparers.
 */
export const UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE = "uaiPropertyValuePreparer";

/**
 * Manifest for property value preparer extensions.
 *
 * Registered preparers are looked up by `forPropertyEditorSchemaAlias` whenever the entity
 * adapter is about to call `setPropertyValue`. The first matching preparer's `prepare` is
 * invoked; editors with no registered preparer use a small default that attempts JSON.parse on
 * string input and returns the result unchanged.
 */
export interface ManifestUaiPropertyValuePreparer extends ManifestBase {
    type: typeof UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE;

    /**
     * The CMS property editor schema alias this preparer handles (e.g. `Umbraco.BlockList`).
     * Matched case-insensitively against the property editor of the value being applied.
     */
    forPropertyEditorSchemaAlias: string;

    /** The preparer API class loader. */
    api: () => Promise<{ default: new () => UaiPropertyValuePreparerApi }>;
}
