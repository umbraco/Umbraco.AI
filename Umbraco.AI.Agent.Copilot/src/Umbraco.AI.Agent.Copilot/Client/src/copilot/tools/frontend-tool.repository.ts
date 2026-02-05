import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
import type { ManifestUaiAgentTool } from "./uai-agent-tool.extension.js";

/**
 * Repository for accessing frontend tool metadata.
 * Queries the extension registry for 'uaiAgentTool' manifests and extracts metadata.
 */
export class UaiFrontendToolRepository
    extends UmbControllerBase
    implements UaiFrontendToolRepositoryApi
{
    constructor(host: UmbControllerHost) {
        super(host);
    }

    /**
     * Get all available frontend tools (tools with api property).
     * @returns Array of frontend tool metadata
     */
    async getTools(): Promise<UaiFrontendToolData[]> {
        const allManifests = umbExtensionsRegistry.getByType('uaiAgentTool') as ManifestUaiAgentTool[];

        // Filter to only tools with api (frontend execution)
        const frontendToolManifests = allManifests.filter((m) => m.api);

        return frontendToolManifests.map((m) => ({
            id: m.meta.toolName,
            name: m.meta.label || m.meta.toolName,
            description: m.meta.description || '',
            scopeId: 'frontend', // All copilot tools use 'frontend' scope
        }));
    }
}

export { UaiFrontendToolRepository as api };
