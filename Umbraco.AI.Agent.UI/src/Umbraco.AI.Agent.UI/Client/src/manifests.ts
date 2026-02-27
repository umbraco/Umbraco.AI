import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST } from "./chat/extensions/uai-agent-tool-renderer.extension.js";
import { UAI_REQUEST_CONTEXT_AGENT_SURFACE_KIND_MANIFEST } from "./chat/extensions/uai-request-context-surface-kind.extension.js";
import { manifests as approvalManifests } from "./chat/manifests/approval.manifests.js";
import { manifests as frontendToolManifests } from "./chat/manifests/frontend-tool.manifests.js";
import { manifests as localizationManifests } from "./lang/manifests.js";

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST,
    UAI_REQUEST_CONTEXT_AGENT_SURFACE_KIND_MANIFEST,
    ...approvalManifests,
    ...frontendToolManifests,
    ...localizationManifests,
];
