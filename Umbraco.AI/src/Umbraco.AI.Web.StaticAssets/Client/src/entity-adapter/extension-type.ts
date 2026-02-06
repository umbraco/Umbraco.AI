/**
 * Entity Adapter Extension Type
 *
 * Defines a custom Umbraco extension type for entity adapters.
 * Adapters are registered via manifests and loaded dynamically.
 */

import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import type { UaiEntityAdapterApi } from "./types.js";

/**
 * Extension type alias for entity adapters.
 */
export const UAI_ENTITY_ADAPTER_EXTENSION_TYPE = "uaiEntityAdapter";

/**
 * Manifest for entity adapter extensions.
 */
export interface ManifestEntityAdapter extends ManifestBase {
    type: typeof UAI_ENTITY_ADAPTER_EXTENSION_TYPE;
    /** The entity type this adapter handles (e.g., "document", "media") */
    forEntityType: string;
    /** The adapter API class loader */
    api: () => Promise<{ default: new () => UaiEntityAdapterApi }>;
}
