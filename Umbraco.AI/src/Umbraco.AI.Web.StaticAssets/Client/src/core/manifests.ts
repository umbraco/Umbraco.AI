import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { iconManifests } from "./icons/manifests.js";
import { manifests as modalManifests } from "./modals/manifests.js";
import { manifests as versionHistoryManifests } from "./version-history/manifests.js";
import { menuItemKinds } from "./menu/manifests.js";
import { dictateTiptapManifests } from "./tiptap/manifests.js";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
	...iconManifests,
	...modalManifests,
	...versionHistoryManifests,
	...menuItemKinds,
	...dictateTiptapManifests,
];
