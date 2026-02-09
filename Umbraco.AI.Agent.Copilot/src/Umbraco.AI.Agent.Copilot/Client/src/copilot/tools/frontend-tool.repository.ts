import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
import type { ManifestUaiAgentTool } from "./uai-agent-tool.extension.js";

/**
 * Repository for accessing frontend tool metadata.
 * Queries the extension registry for 'uaiAgentTool' manifests and extracts metadata.
 */
export class UaiFrontendToolRepository extends UmbControllerBase implements UaiFrontendToolRepositoryApi {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    /**
     * Get all available frontend tools (tools with parameters defined).
     * Backend tools don't define parameters - they come from the server.
     * @returns Array of frontend tool metadata
     */
    async getTools(): Promise<UaiFrontendToolData[]> {
        const frontendToolManifests = umbExtensionsRegistry.getByTypeAndFilter<"uaiAgentTool", ManifestUaiAgentTool>(
            "uaiAgentTool",
            (m) => Boolean(m.api),
        );

        return frontendToolManifests.map((m) => ({
            id: m.meta.toolName,
            name: m.meta.label || m.meta.toolName,
            description: m.meta.description || "",
            scopeId: m.meta.scope || "general", // Read scope from manifest, default to 'general'
        }));
    }
}

export { UaiFrontendToolRepository as api };
