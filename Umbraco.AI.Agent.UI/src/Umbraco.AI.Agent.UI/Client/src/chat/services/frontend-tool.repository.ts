import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
import type { ManifestUaiAgentFrontendTool } from "../extensions/uai-agent-frontend-tool.extension.js";

/**
 * Repository for accessing frontend tool metadata.
 *
 * Queries the extension registry for 'uaiAgentFrontendTool' manifests and extracts
 * metadata for display in backoffice tool pickers (e.g., agent permissions workspace).
 *
 * This is generic infrastructure that discovers tools from any surface (copilot, chat, etc.)
 * that registers frontend tools via the extension system.
 */
export class UaiFrontendToolRepository extends UmbControllerBase implements UaiFrontendToolRepositoryApi {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    async getTools(): Promise<UaiFrontendToolData[]> {
        const frontendToolManifests = umbExtensionsRegistry.getByTypeAndFilter<
            "uaiAgentFrontendTool",
            ManifestUaiAgentFrontendTool
        >("uaiAgentFrontendTool", (m) => Boolean(m.api));

        return frontendToolManifests.map((m) => ({
            id: m.meta.toolName,
            name: m.meta.toolName,
            description: m.meta.description || "",
            scopeId: m.meta.scope || "general",
        }));
    }
}

export { UaiFrontendToolRepository as api };
