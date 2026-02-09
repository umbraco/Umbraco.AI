import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST } from "./chat/manifests/tool-renderer-kind.manifests.js";
import { manifests as approvalManifests } from "./chat/manifests/approval.manifests.js";
import { manifests as localizationManifests } from "./lang/manifests.js";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST,
    ...approvalManifests,
    ...localizationManifests,
];
