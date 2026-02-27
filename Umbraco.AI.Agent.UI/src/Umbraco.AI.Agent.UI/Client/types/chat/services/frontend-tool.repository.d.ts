import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
/**
 * Repository for accessing frontend tool metadata.
 *
 * Queries the extension registry for 'uaiAgentFrontendTool' manifests and extracts
 * metadata for display in backoffice tool pickers (e.g., agent permissions workspace).
 *
 * This is generic infrastructure that discovers tools from any surface (copilot, chat, etc.)
 * that registers frontend tools via the extension system.
 */
export declare class UaiFrontendToolRepository extends UmbControllerBase implements UaiFrontendToolRepositoryApi {
    constructor(host: UmbControllerHost);
    getTools(): Promise<UaiFrontendToolData[]>;
}
export { UaiFrontendToolRepository as api };
//# sourceMappingURL=frontend-tool.repository.d.ts.map