import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as modalManifests } from "./modals/manifests.js";
import { manifests as versionHistoryManifests } from "./version-history/manifests.js";
import { menuItemKinds } from "./menu/manifests.js";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
	...modalManifests,
	...versionHistoryManifests,
	...menuItemKinds,
];
