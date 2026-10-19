/**
 * Entity Adapter Manifests
 *
 * Registers built-in entity adapters via the Umbraco extension manifest system.
 */

import type { ManifestEntityAdapter } from "../extension-type.js";
import { UAI_ENTITY_ADAPTER_EXTENSION_TYPE } from "../extension-type.js";

export const entityAdapterManifests: ManifestEntityAdapter[] = [
    {
        type: UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
        alias: "UmbracoAI.EntityAdapter.Document",
        name: "Document Entity Adapter",
        forEntityType: "document",
        api: () => import("./document.adapter.js"),
    },
    {
        type: UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
        alias: "UmbracoAI.EntityAdapter.Media",
        name: "Media Entity Adapter",
        forEntityType: "media",
        api: () => import("./media.adapter.js"),
    },
    {
        type: UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
        alias: "UmbracoAI.EntityAdapter.Block",
        name: "Block Entity Adapter",
        forEntityType: "block",
        // Prompts look this adapter up by entity type to send a block's values, but it is kept out of
        // the Copilot context selector's auto-detection — see #343.
        // 1) A block's edit view opens in a modal that overlays the copilot, so a user can't chat about
        //    a block while it's open anyway; this is blocked on CMS shipping a drawer UI.
        // 2) With inline block editing, the CMS keeps a live workspace context per rendered block
        //    (not just the focused one), so every block on the page gets detected as "active" at once.
        // Drop this flag once both issues are resolved.
        detectable: false,
        api: () => import("./block.adapter.js"),
    },
];
