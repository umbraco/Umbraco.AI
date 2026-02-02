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
		alias: "UmbracoAi.EntityAdapter.Document",
		name: "Document Entity Adapter",
		forEntityType: "document",
		api: () => import("./document.adapter.js"),
	},
];
